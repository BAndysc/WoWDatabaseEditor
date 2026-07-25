#version 450

// Procedurally drawn (no texture assets) editor gizmo icons. vP spans [-1, 1] across the
// billboard quad; each icon is built from small SDF primitives so it stays crisp at any size.

#define PI 3.14159265358979323846

layout(location = 0) in vec2 vP;
layout(location = 1) flat in int iconType;
layout(location = 2) flat in vec4 tint;
layout(location = 3) flat in int vPickId;

layout(location = 0) out vec4 FragColor;
// The scene texture has a second (uint) attachment used for object picking. Write the picker id of
// the entity this icon represents (0 for the non-selectable camera icon) so clicking an icon selects
// it. The pipeline always has color-write enabled on this attachment, so it must be written
// explicitly - leaving it undefined would make picking through an icon read garbage.
layout(location = 1) out uint ObjectIndexOutputBuffer;

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

// Antialiased coverage from a signed distance (negative == inside).
float fill(float d)
{
    float w = fwidth(d) + 1e-5;
    return clamp(0.5 - d / w, 0.0, 1.0);
}

// Antialiased stroke of half-width t around the isoline d == 0.
float stroke(float d, float t) { return fill(abs(d) - t); }

// A sun: filled core + eight radiating rays.
float lightIcon(vec2 p)
{
    float a = fill(sdCircle(p, 0.30));
    for (int i = 0; i < 8; i++)
    {
        float ang = float(i) * (PI / 4.0);
        vec2 dir = vec2(cos(ang), sin(ang));
        a = max(a, stroke(sdSegment(p, dir * 0.46, dir * 0.78), 0.05));
    }
    return a;
}

// A little camera: rectangular body, a lens hole, a lens hood and a viewfinder bump.
float cameraIcon(vec2 p)
{
    float body = fill(sdBox(p - vec2(-0.06, 0.0), vec2(0.32, 0.22)));
    float hood = fill(sdBox(p - vec2(0.32, 0.0), vec2(0.11, 0.13)));
    float bump = fill(sdBox(p - vec2(-0.20, 0.26), vec2(0.10, 0.05)));
    float a = max(max(body, hood), bump);

    // punch the lens hole out of the body so the camera reads clearly
    float lens = fill(sdCircle(p - vec2(-0.06, 0.0), 0.11));
    a *= (1.0 - 0.85 * lens);
    return a;
}

// A decal: square frame with a central splat and a couple of droplets (a projected "sticker").
float decalIcon(vec2 p)
{
    float a = stroke(sdBox(p, vec2(0.34)), 0.05);
    a = max(a, fill(sdCircle(p, 0.12)));
    a = max(a, fill(sdCircle(p - vec2(0.18, 0.18), 0.05)));
    a = max(a, fill(sdCircle(p - vec2(-0.17, -0.16), 0.045)));
    return a;
}

void main()
{
    float a;
    if (iconType == 0)
        a = lightIcon(vP);
    else if (iconType == 1)
        a = cameraIcon(vP);
    else
        a = decalIcon(vP);

    if (a <= 0.001)
        discard;

    FragColor = vec4(tint.rgb, a * tint.a);
    ObjectIndexOutputBuffer = uint(vPickId);
}
