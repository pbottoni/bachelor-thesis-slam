import pandas as pd
import numpy as np
from csv import reader
import matplotlib.pyplot as plt
import csv
import sys


def convert(file):
    data=[]
    with open(file, 'r') as camera:
        c_reader =reader(camera)
        for row in c_reader:
            #print(row)
            if len(row)>0 and (row[0]=='Pose'):
                new_row=[float(i) for i in row[1:]]
                data.append(new_row)
    return data


def convertOrb(file):
    data=[]
    with open(file, 'r') as camera:
        c_reader =camera.read().splitlines()
        for row in c_reader:
            row_=row.split()
            new_row=[float(i) for i in row_[1:]]
            data.append(new_row)
    return data
##header=['Stream Type','x', 'y', 'z', 'rx','ry','rz']

camera=sys.argv[1]

orb = sys.argv[2]

data_camera=convert(camera)
data_camera=np.asarray(data_camera)



data_orb=convertOrb(orb)
data_orb=np.asarray(data_orb)

fig = plt.figure()

plt.xlabel('x-axis')
plt.ylabel('y-axis')

##print(data_oculus)
camera_mean=np.max(np.abs(data_camera),0)
normal_camera=data_camera/camera_mean

orb_mean=np.max(np.abs(data_orb),0)
normal_orb=data_orb/orb_mean
##print(normal_oculus)


plt.scatter(normal_camera[:,2], -normal_camera[:,0],s=1,label="camera",color="b")

plt.scatter(normal_orb[:,2], normal_orb[:,0],s=1,label="ORB SLAM 3",color="orange")


plt.scatter(normal_camera[10,2], -normal_camera[10,0],s=60,color="deepskyblue")

plt.scatter(normal_orb[0,2], normal_orb[0,0],s=60,color="y")


plt.scatter(normal_camera[-2,2], -normal_camera[-2,0],s=60,color="midnightblue")

plt.scatter(normal_orb[-2,2], normal_orb[-2,0],s=60,color="olive")

plt.legend()
plt.savefig("output_"+str(camera)[22:-4]+".png")
