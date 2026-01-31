#version 330

in vec2 fragTexCoord;
out vec4 finalColor;

uniform sampler2D texture0;
uniform bool enabled;
uniform float px;
uniform float py;

void main() {
    vec2 uv = fragTexCoord;

    if (!enabled) {
        finalColor = texture(texture0, uv);
        return;
    }

    uv.x = floor(uv.x / px) * px;
    uv.y = floor(uv.y / py) * py;

    finalColor = texture(texture0, uv);
}