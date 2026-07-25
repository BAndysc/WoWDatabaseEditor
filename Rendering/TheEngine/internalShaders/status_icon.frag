#version 450

// Procedurally drawn (SDF) NPC status icons - crisp at any distance/zoom, no texture assets
// (the client's GossipFrame BLPs are 32px and look terrible scaled up). Composition is
// distance-based (min = union, max(d, -c) = punch), so one dark outline band can be dilated
// around the whole glyph for readability against the world.

layout(location = 0) in vec2 TexCoords;
layout(location = 1) flat in int vIconType; // StatusIconsManager.StatusIcon

layout(location = 0) out vec4 FragColor;

float sdCircle(vec2 p, float r) { return length(p) - r; }

float sdBox(vec2 p, vec2 b)
{
    vec2 d = abs(p) - b;
    return length(max(d, 0.0)) + min(max(d.x, d.y), 0.0);
}

float sdSegment(vec2 p, vec2 a, vec2 b)
{
    vec2 pa = p - a, ba = b - a;
    float h = clamp(dot(pa, ba) / dot(ba, ba), 0.0, 1.0);
    return length(pa - ba * h);
}

float sdTriangle(vec2 p, vec2 p0, vec2 p1, vec2 p2)
{
    vec2 e0 = p1 - p0, e1 = p2 - p1, e2 = p0 - p2;
    vec2 v0 = p - p0, v1 = p - p1, v2 = p - p2;
    vec2 pq0 = v0 - e0 * clamp(dot(v0, e0) / dot(e0, e0), 0.0, 1.0);
    vec2 pq1 = v1 - e1 * clamp(dot(v1, e1) / dot(e1, e1), 0.0, 1.0);
    vec2 pq2 = v2 - e2 * clamp(dot(v2, e2) / dot(e2, e2), 0.0, 1.0);
    float s = sign(e0.x * e2.y - e0.y * e2.x);
    vec2 d = min(min(vec2(dot(pq0, pq0), s * (v0.x * e0.y - v0.y * e0.x)),
                     vec2(dot(pq1, pq1), s * (v1.x * e1.y - v1.y * e1.x))),
                     vec2(dot(pq2, pq2), s * (v2.x * e2.y - v2.y * e2.x)));
    return -sqrt(d.x) * sign(d.y);
}

// arc of radius ra covering +-aperture around +y; sc = (sin, cos) of the aperture half-angle
float sdArc(vec2 p, vec2 sc, float ra)
{
    p.x = abs(p.x);
    return (sc.y * p.x > sc.x * p.y) ? length(p - sc * ra) : abs(length(p) - ra);
}

// antialiased coverage from a signed distance (negative == inside)
float fill(float d)
{
    float w = fwidth(d) + 1e-5;
    return clamp(0.5 - d / w, 0.0, 1.0);
}

// exclamation mark - available quest
float questGiver(vec2 p)
{
    float bar = sdSegment(p, vec2(0.0, 0.30), vec2(0.0, -0.06)) - 0.115;
    float dot = sdCircle(p - vec2(0.0, -0.33), 0.11);
    return min(bar, dot);
}

// question mark - quest turn-in
float questEnder(vec2 p)
{
    // the hook: an arc covering ~245 deg, opening towards the lower-left (where the stem hangs)
    vec2 q = p - vec2(0.0, 0.13);
    float cs = 0.70710678, sn = 0.70710678; // rotate 45 deg so the covered arc centers upper-right
    q = mat2(cs, sn, -sn, cs) * q;
    float arc = sdArc(q, vec2(sin(2.14), cos(2.14)), 0.21) - 0.10;
    float stem = sdSegment(p, vec2(0.02, -0.07), vec2(0.02, -0.15)) - 0.09;
    float dot = sdCircle(p - vec2(0.0, -0.36), 0.105);
    return min(min(arc, stem), dot);
}

// speech bubble with three dots - gossip
float gossip(vec2 p)
{
    float bubble = sdBox(p - vec2(0.0, 0.06), vec2(0.26, 0.17)) - 0.12; // rounded rect
    float tail = sdSegment(p, vec2(-0.10, -0.14), vec2(-0.22, -0.38)) - 0.06;
    float d = min(bubble, tail);
    float dots = sdCircle(p - vec2(-0.14, 0.06), 0.045);
    dots = min(dots, sdCircle(p - vec2(0.0, 0.06), 0.045));
    dots = min(dots, sdCircle(p - vec2(0.14, 0.06), 0.045));
    return max(d, -dots); // punch the dots out of the bubble
}

// crown - spawn group formation leader (an editor overlay, not an NPC status)
float crown(vec2 p)
{
    float band = sdBox(p - vec2(0.0, -0.18), vec2(0.26, 0.09)) - 0.02; // rounded base band
    float baseY = -0.07;                                               // spikes rise from the band top
    float left  = sdTriangle(p, vec2(-0.28, baseY), vec2(-0.10, baseY), vec2(-0.19, 0.24));
    float mid   = sdTriangle(p, vec2(-0.13, baseY), vec2( 0.13, baseY), vec2( 0.00, 0.34));
    float right = sdTriangle(p, vec2( 0.10, baseY), vec2( 0.28, baseY), vec2( 0.19, 0.24));
    return min(band, min(mid, min(left, right)));
}

// connected waypoints (a little route of node dots) - creature has a movement path assigned
float waypoints(vec2 p)
{
    vec2 n0 = vec2(-0.30, -0.28);
    vec2 n1 = vec2( 0.02, -0.02);
    vec2 n2 = vec2( 0.30,  0.26);
    float line = min(sdSegment(p, n0, n1), sdSegment(p, n1, n2)) - 0.035; // the path
    float dots = min(min(sdCircle(p - n0, 0.085), sdCircle(p - n1, 0.085)),
                     sdCircle(p - n2, 0.085));                            // the node handles
    return min(line, dots);
}

// gear - scripted AI
float gear(vec2 p)
{
    float ring = abs(sdCircle(p, 0.22)) - 0.085;
    float teeth = 1e9;
    for (int i = 0; i < 8; i++)
    {
        float a = float(i) * 0.78539816;
        vec2 dir = vec2(cos(a), sin(a));
        teeth = min(teeth, sdSegment(p, dir * 0.24, dir * 0.35) - 0.055);
    }
    return min(ring, teeth);
}

void main()
{
    // uv (y down) -> icon space (y up), quad spans [-0.5, 0.5]^2
    vec2 p = vec2(TexCoords.x - 0.5, 0.5 - TexCoords.y);

    float d;
    vec3 tint;
    if (vIconType == 0)      { d = questGiver(p); tint = vec3(1.00, 0.78, 0.09); } // gold !
    else if (vIconType == 1) { d = questEnder(p); tint = vec3(1.00, 0.78, 0.09); } // gold ?
    else if (vIconType == 2) { d = gossip(p);     tint = vec3(0.98, 0.97, 0.92); } // white bubble
    else if (vIconType == 3) { d = gear(p);       tint = vec3(0.45, 0.78, 1.00); } // blue gear
    else if (vIconType == 4) { d = crown(p);      tint = vec3(1.00, 0.80, 0.15); } // gold crown
    else                     { d = waypoints(p);  tint = vec3(0.40, 1.00, 0.55); } // green route

    float body = fill(d);
    float withOutline = fill(d - 0.045); // dilated: the dark outline band around the glyph
    if (withOutline <= 0.004)
        discard;

    vec3 color = mix(vec3(0.10, 0.08, 0.03), tint, body);
    FragColor = vec4(color, withOutline);
}
