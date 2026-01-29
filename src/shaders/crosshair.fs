#version 330

in vec2 fragTexCoord;
out vec4 finalColor;

uniform sampler2D texture0;
uniform float aspect;
uniform bool interacting;

vec2 center = vec2(0.5, 0.5);
float radius = 0.004;
float feather = 0.001;

void main() {
    vec4 screen = texture(texture0, fragTexCoord);
    vec2 scaled = vec2((fragTexCoord.x - center.x) * aspect, fragTexCoord.y - center.y);
    float mask = smoothstep(radius, radius - feather, length(scaled));

    vec3 inverted = (vec3(1.0) - screen.rgb) * (interacting ? 1 : 0.5);
    vec3 result = mix(screen.rgb, inverted, mask);

    finalColor = vec4(result, 1.0);
}
