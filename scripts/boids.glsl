#[compute]
#version 450

struct BoidWeights {
    float separation;
    float alignment;
    float cohesion;
};

layout(local_size_x = 64, local_size_y = 1, local_size_z = 1) in;

layout(set = 0, binding = 0, std430) restrict buffer TurningVectors {
    vec2 data[];
} turningVectors;

layout(set = 0, binding = 1, std430) restrict buffer BoidPositionBuffer {
    vec2 data[];
} boidPosition;

layout(set = 0, binding = 2, std430) restrict buffer BoidVelocityBuffer {
    vec2 data[];
} boidVelocity;

layout(set = 0, binding = 3, std430) restrict buffer BoidGoalBuffer {
    vec2 data[];
} boidGoals;

layout(set = 0, binding = 4, std430) restrict buffer NumBoidBuffer {
    uint count;
} numBoid;

layout(set = 0, binding = 5, std430) restrict buffer BoidGridIndicesBuffer {
    uint data[];
} boidGridIndices;

layout(set = 1, binding = 0, std430) restrict buffer SeparatingGridInfoBuffer {
    float distance;
} separatingGridInfo;

layout(set = 2, binding = 0, std430) restrict buffer LocalGridBuffer {
    vec4 data[];
} localGrid;

layout(set = 2, binding = 1, std430) restrict buffer LocalGridInfoBuffer {
    uint width;
    uint height;
    float distance;
    uvec2 cellData[];
} localGridInfo;

layout(set = 3, binding = 0, std430) restrict buffer TurningWeights {
    BoidWeights weights;
    float padding;
    float goalSeeking[];
} turningWeight;

shared vec4 sharedBoids[1024];
shared uint sharedBoidCount;
shared float sharedSeparatingGridDistance;
shared float sharedLocalGridDistance;
shared uint sharedLocalGridWidth;
shared uint sharedLocalGridHeight;

vec2 GoalSeeking(vec2 boidPos, vec2 boidGoal);

void main() {
    uint boidInfoIndex = gl_GlobalInvocationID.x;
    if (boidInfoIndex >= numBoid.count) {
        return;
    }

    if (gl_LocalInvocationID.x == 0) {
        sharedBoidCount = 0;
        sharedSeparatingGridDistance = separatingGridInfo.distance;
        sharedLocalGridDistance = localGridInfo.distance;
        sharedLocalGridWidth = localGridInfo.width;
        sharedLocalGridHeight = localGridInfo.height;
    }

    barrier();

    vec2 boidPos = boidPosition.data[boidInfoIndex];
    uint localGridX = boidGridIndices.data[boidInfoIndex] % sharedLocalGridWidth;
    uint localGridY = boidGridIndices.data[boidInfoIndex] / sharedLocalGridWidth;

    for (int y = -1; y <= 1; y++) {
        for (int x = -1; x <= 1; x++) {
            int currentY = int(localGridY) + y;
            int currentX = int(localGridX) + x;

            if (currentY >= 0 && currentY < sharedLocalGridHeight && currentX >= 0 && currentX < sharedLocalGridWidth) {
                uint neighborCellIndex = currentY * sharedLocalGridWidth + currentX;
                uint offset = localGridInfo.cellData[neighborCellIndex].x;
                uint count = localGridInfo.cellData[neighborCellIndex].y;

                for (uint i = gl_LocalInvocationID.x; i < count; i += gl_WorkGroupSize.x) {
                    uint write_index = atomicAdd(sharedBoidCount, 1);
                    if (write_index < 512) {
                        sharedBoids[write_index] = localGrid.data[offset + i];
                    }
                }
            }
        }
    }

    barrier();

    vec2 separationVector = vec2(0);
    vec2 alignmentVector = vec2(0);
    vec2 cohesionVector = vec2(0);
    uint localCount = 0;

    uint neighbors_in_cache = sharedBoidCount;
    for (uint i = 0; i < neighbors_in_cache; i++) {
        vec2 otherBoidPos = sharedBoids[i].xy;
        vec2 toOther = otherBoidPos - boidPos;
        float distSq = dot(toOther, toOther);

        if (distSq > 0.0001) {
            if (distSq < sharedLocalGridDistance) {
                localCount++;
                if (distSq < sharedSeparatingGridDistance) {
                 separationVector -= toOther / (distSq + 0.0001);
                }
                alignmentVector += sharedBoids[i].zw;
                cohesionVector += otherBoidPos;
            }
        }
    }
    
    vec2 finalVector = separationVector * turningWeight.weights.separation;

    if (localCount > 0) {
        alignmentVector /= localCount;
        if (dot(alignmentVector, alignmentVector) > 0.0) {
            finalVector += normalize(alignmentVector) * turningWeight.weights.alignment;
        }

        cohesionVector = cohesionVector / localCount - boidPos;
        if (dot(cohesionVector, cohesionVector) > 0.0) {
            finalVector += normalize(cohesionVector) * turningWeight.weights.cohesion;
        }
    }

    vec2 goalVector = GoalSeeking(boidPos, boidGoals.data[boidInfoIndex]);
    finalVector += goalVector * turningWeight.goalSeeking[boidInfoIndex];

    turningVectors.data[boidInfoIndex] = finalVector;
}

vec2 GoalSeeking(vec2 boidPos, vec2 boidGoal) {
    vec2 neededDirection = normalize(boidGoal - boidPos);
	return neededDirection;
}