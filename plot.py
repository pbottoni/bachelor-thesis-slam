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
oculus=sys.argv[2]
tracker=sys.argv[3]
orb = sys.argv[4]

data_camera=convert(camera)
data_camera=np.asarray(data_camera)

data_oculus=convert(oculus)
data_oculus=np.asarray(data_oculus)
##round_data_oculus=np.around(data_oculus,0)
##data_oculus=data_oculus-round_data_oculus
##data_oculus=data_oculus*10

data_tracker=convert(tracker)
data_tracker=np.asarray(data_tracker)

data_orb=convertOrb(orb)
data_orb=np.asarray(data_orb)

fig = plt.figure()

plt.xlabel('x-axis')
plt.ylabel('y-axis')

##print(data_oculus)
camera_mean=np.max(np.abs(data_camera),0)
normal_camera=data_camera/camera_mean
data_oculus=data_oculus-data_oculus[10,:][None,:]
oculus_mean=np.max(np.abs(data_oculus),0)
normal_oculus=data_oculus/oculus_mean
data_tracker=data_tracker-data_tracker[10,:][None,:]
tracker_mean=np.max(np.abs(data_tracker),0)
normal_tracker=data_tracker/tracker_mean
orb_mean=np.max(np.abs(data_orb),0)
normal_orb=data_orb/orb_mean
##print(normal_oculus)

plt.scatter(normal_tracker[:,0], normal_tracker[:,2],s=1,label="tracker",color="r")
plt.scatter(normal_camera[:,0], normal_camera[:,2],s=1,label="camera",color="b")
plt.scatter(normal_oculus[:,2], -normal_oculus[:,0],s=1,label="oculus",color="g")
plt.scatter(-normal_orb[:,0], normal_orb[:,2],s=1,label="ORB SLAM 3",color="orange")


plt.scatter(normal_tracker[10,0], normal_tracker[10,2],s=60,color="salmon")
plt.scatter(normal_camera[10,0], normal_camera[10,2],s=60,color="deepskyblue")
plt.scatter(normal_oculus[10,2], -normal_oculus[10,0],s=60,color="lime")
plt.scatter(-normal_orb[0,0], normal_orb[0,2],s=60,color="y")

plt.scatter(normal_tracker[-2,0], normal_tracker[-2,2],s=60,color="maroon")
plt.scatter(normal_camera[-2,0], normal_camera[-2,2],s=60,color="midnightblue")
plt.scatter(normal_oculus[-2,2], -normal_oculus[-2,0],s=60,color="darkgreen")
plt.scatter(-normal_orb[-2,0], normal_orb[-2,2],s=60,color="olive")

plt.legend()
plt.savefig("output_"+str(camera)[22:-4]+".png")
