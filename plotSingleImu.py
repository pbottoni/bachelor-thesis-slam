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
        j=0
        for row in c_reader:
            #print(row)

            if len(row)>0 and j==1:
                new_row=[float(i) for i in row]
                data.append(new_row)
            j=1
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


fig = plt.figure()

plt.xlabel('x-axis')
plt.ylabel('y-axis')

##print(data_oculus)
# camera_mean=np.max(np.abs(data_camera),0)
normal_camera=data_camera

##print(normal_oculus)

plt.scatter(normal_camera[:,0], normal_camera[:,1]+10,s=1,label="rad_x",color="b")
plt.scatter(normal_camera[:,0], normal_camera[:,2]+15,s=1,label="rad_y",color="r")
plt.scatter(normal_camera[:,0], normal_camera[:,3]+20,s=1,label="rad_z",color="g")
plt.scatter(normal_camera[:,0], normal_camera[:,4]+25,s=1,label="x",color="y")
plt.scatter(normal_camera[:,0], normal_camera[:,5]+30,s=1,label="y",color="orange")
plt.scatter(normal_camera[:,0], normal_camera[:,6]+35,s=1,label="z",color="k")



plt.legend()
plt.savefig("output_single_IMU"+str(camera)[22:-14]+".png")
