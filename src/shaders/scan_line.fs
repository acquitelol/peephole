#version 330

in vec2 fragTexCoord;
out vec4 finalColor;

uniform sampler2D texture0;
uniform float intensity;
uniform float thickness;

void main() {
    vec2 uv = fragTexCoord;

    float scan = sin(uv.y * 3.14159 / thickness);
    scan = clamp(scan, 0.0, 1.0);

    vec3 color = texture(texture0, uv).rgb;
    color *= mix(1.0, 1.0 - intensity, scan);

    finalColor = vec4(color, 1.0);
}