import numpy as np
import sys
import os
import math

def rotate(origin, point, angle):
    """
    Rotate a point counterclockwise by a given angle around a given origin.

    The angle should be given in radians.
    """
    ox, oy, oz = origin
    px, py, pz = point

    qx = ox + math.cos(angle) * (px - ox) - math.sin(angle) * (py - oy)
    qy=py
    qz = oz + math.sin(angle) * (pz - oz) + math.cos(angle) * (pz - oz)
    return qx, qy, qz

def quaternion_rotation_matrix(Q):
    """
    Covert a quaternion into a full three-dimensional rotation matrix.
 
    Input
    :param Q: A 4 element array representing the quaternion (q0,q1,q2,q3) 
 
    Output
    :return: A 3x3 element matrix representing the full 3D rotation matrix. 
             This rotation matrix converts a point in the local reference 
             frame to a point in the global reference frame.
    """
    # Extract the values from Q
    q0 = Q[0]
    q1 = Q[1]
    q2 = Q[2]
    q3 = Q[3]
     
    # First row of the rotation matrix
    r00 = 2 * (q0 * q0 + q1 * q1) - 1
    r01 = 2 * (q1 * q2 - q0 * q3)
    r02 = 2 * (q1 * q3 + q0 * q2)
     
    # Second row of the rotation matrix
    r10 = 2 * (q1 * q2 + q0 * q3)
    r11 = 2 * (q0 * q0 + q2 * q2) - 1
    r12 = 2 * (q2 * q3 - q0 * q1)
     
    # Third row of the rotation matrix
    r20 = 2 * (q1 * q3 - q0 * q2)
    r21 = 2 * (q2 * q3 + q0 * q1)
    r22 = 2 * (q0 * q0 + q3 * q3) - 1
     
    # 3x3 rotation matrix
    rot_matrix = np.array([[r00, r01, r02],
                           [r10, r11, r12],
                           [r20, r21, r22]])
                            
    return rot_matrix


data_name=sys.argv[1]
data=np.loadtxt(data_name,dtype=float)

max_jump=np.array([0,0,0])
index=0
for i in range(1,len(data)):
    if data[i,1]==0:
        continue
    if i ==len(data)-1:
        continue

    jump=data[i,1:4]-data[i-1,1:4]
    if np.linalg.norm(jump)>np.linalg.norm(max_jump):
        max_jump=jump
        index=i
print(index)
# print(max_jump)
ab=data[index-2,1:4]-data[index-1,1:4]
cd=data[index,1:4]-data[index+1,1:4]
offset=data[index,1:4]-data[index-1,1:4]+ab

unit_vector_1 = cd / np.linalg.norm(ab)
unit_vector_2 = ab / np.linalg.norm(cd)
dot_product = np.dot(unit_vector_1, unit_vector_2)

angle = np.arccos(dot_product)
angle=math.radians(angle)
print(angle)


for i in range(index,len(data)):
    data[i,1:4]=data[i,1:4]-offset


for i in range(index,len(data)):
    data[i,1:4]=rotate(data[index,1:4],data[i,1:4],angle)

np.savetxt(data_name[:-4]+"_joined.txt",data)