#include <raylib.h>
#include <stdio.h>

void scale_model_uv(Model *model, int *s) {
  for (int i = 0; i < model->meshCount; ++i) {
    Mesh *mesh = &model->meshes[i];
    if (!mesh->texcoords)
      continue;
    int su = s[i * 2 + 0];
    int sv = s[i * 2 + 1];

    for (int j = 0; j < mesh->vertexCount; ++j) {
      mesh->texcoords[j * 2 + 0] *= su;
      mesh->texcoords[j * 2 + 1] *= sv;
    }

    UpdateMeshBuffer(*mesh, 1, mesh->texcoords,
                     mesh->vertexCount * 2 * sizeof(float), 0);
  }
}