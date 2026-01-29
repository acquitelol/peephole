#version 330

in vec3 vertexPosition;
in vec2 vertexTexCoord;
in vec3 vertexNormal;
in vec4 vertexColor;

uniform mat4 mvp;
uniform mat4 model;

out vec2 fragTexCoord;
out vec4 fragColor;
out vec3 fragNormal;
out vec3 fragPos;

void main() {
    fragTexCoord = vertexTexCoord;
    fragColor = vertexColor;

    vec4 worldPos = model * vec4(vertexPosition, 1.0);
    fragPos = worldPos.xyz;

    mat3 normalMatrix = transpose(inverse(mat3(model)));
    fragNormal = normalize(normalMatrix * vertexNormal);

    gl_Position = mvp * vec4(vertexPosition, 1.0);
}