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


data_camera=convert(camera)
data_camera=np.asarray(data_camera)

#print(data_camera[:,2])

fig = plt.figure()

plt.xlabel('x-axis')
plt.ylabel('y-axis')

##print(data_oculus)
#camera_mean=np.max(np.abs(data_camera),0)
#normal_camera=data_camera/camera_mean

##print(normal_oculus)

plt.scatter(data_camera[:,2], data_camera[:,0],s=1,label="camera",color="b")

plt.scatter(data_camera[10,2], data_camera[10,0],s=60,color="deepskyblue")

plt.scatter(data_camera[-2,2], data_camera[-2,0],s=60,color="midnightblue")

plt.legend()
plt.savefig("output_single_"+str(camera)[16:-4]+".png")
