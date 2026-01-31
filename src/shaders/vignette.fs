#version 330

in vec2 fragTexCoord;
out vec4 finalColor;

uniform sampler2D texture0;
uniform bool enabled;
float strength = 0.7;
float radius = 0.9;
float softness = 0.9;

void main() {
    vec4 color = texture(texture0, fragTexCoord);
    if (!enabled) {
        finalColor = color;
        return;
    }

    vec2 uv = fragTexCoord * 2.0 - 1.0;
    float dist = length(uv);

    float vignette = smoothstep(radius, radius - softness, dist);
    vignette = mix(1.0, vignette, strength);

    finalColor = vec4(color.rgb * vignette, color.a);
}
