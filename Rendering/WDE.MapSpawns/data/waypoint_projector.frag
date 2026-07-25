#version 450
#include "../internalShaders/theengine.cginc"

// Solid path color written into the projector overlay. Alpha = coverage: 1 where the ribbon is,
// 0 (cleared) everywhere else, so the decal/surface projection only paints the actual path.
layout(location = 0) in vec4 vColor;
layout(location = 0) out vec4 FragColor;

void main()
{
    FragColor = vec4(vColor.rgb, 1.0);
}
