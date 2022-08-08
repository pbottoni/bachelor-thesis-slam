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
            if len(row)>0 and (row[0]!="#timestamp [ns]"):
                new_row=[float(i) for i in row[:]]
                data.append(new_row)
    return data




accel=sys.argv[1]
accel2=sys.argv[2]

data_camera=convert(accel)
data_camera=np.asarray(data_camera)

data_camera2=convert(accel2)
data_camera2=np.asarray(data_camera2)




fig = plt.figure()

plt.xlabel('x-axis')
plt.ylabel('y-axis')

#print(data_camera[:,0])


plt.plot(data_camera[:,0], data_camera[:,5],label="inter",color="r")
plt.plot(data_camera2[:,0], data_camera2[:,5],label="not",color="b")





plt.legend()
plt.savefig("accel.png")
