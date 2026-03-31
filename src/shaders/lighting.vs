#version 330

in vec3 vertexPosition;
in vec2 vertexTexCoord;
in vec3 vertexTangent;
in vec3 vertexNormal;
in vec4 vertexColor;

uniform mat4 mvp;
uniform mat4 matModel;

out vec2 fragTexCoord;
out vec4 fragColor;
out vec3 fragNormal;
out vec3 fragPos;
out mat4 fragModel;
out mat3 TBN;

void main() {
    fragTexCoord = vertexTexCoord;
    fragColor = vertexColor;
    vec4 worldPos = matModel * vec4(vertexPosition, 1.0);
    fragPos = worldPos.xyz;
    fragModel = matModel;

    mat3 normalMatrix = transpose(inverse(mat3(matModel)));
    // fragNormal = normalize(normalMatrix * vertexNormal);

    vec3 N = normalize(normalMatrix * vertexNormal);
    vec3 T = normalize(normalMatrix * vertexTangent);
    T = normalize(T - dot(T, N) * N);
    vec3 B = cross(N, T);

    TBN = mat3(T, B, N);
    fragNormal = N;

    gl_Position = mvp * vec4(vertexPosition, 1.0);
}
