import argparse
import os,sys,math
# from logzero import logger
# import pdal,json
# from pyproj import CRS
import open3d as o3d
import numpy as np
import laspy

def round_up_to_nearest_100(num):
    return math.ceil(num / 100) * 100

def ply2las(input_file,output_file,epsg=None):
    """
        PLY files are normally using RGB unsigned char values from 0-255
        LAS RGB requires 0-65535 values 
    """
    print(" ply2las : converting "+input_file + " to "+ output_file )
    info = {}
    pcd = o3d.io.read_point_cloud(input_file)
    points = np.asarray(pcd.points)

    # open3D uses 0.0->1.0 floating points values, LAS requires unsigned 16 bit colors
    rgb = np.asarray(pcd.colors) * 65535
    rgb = rgb.astype(np.uint16)

    # generate the bounding box information to compress the points
    # down to int32, the rescale values help to reduce information
    # stored to reduce the size of the file
    info["min_x"] = np.min(points[:,0])
    info["min_y"] = np.min(points[:,1])
    info["min_z"] = np.min(points[:,2])
    info["max_x"] = np.max(points[:,0])
    info["max_y"] = np.max(points[:,1])
    info["max_z"] = np.max(points[:,2])

    # rescale the height values so they can be scaled down for
    # more efficient storage
    intensities = (points[:,2]-info["min_z"])/(info["max_z"]-info["min_z"])*65535
    intensities = intensities.astype(np.uint16)

    # most applications support version 1.2, so keep it like that
    # point format tells the reader what other data is stored as well
    # such as classification points, returns, etc ...
    header = laspy.LasHeader(version="1.2", point_format=2) # LAS point format 2 supports color

    # CRS refers to the world coordinate system used
    # applications such as OpenMVG can export clouds as ECEF or UTM coordinates
    # which have specific CRS
    # header.add_crs(CRS.from_epsg(epsg))
    header.generating_software = "FOSS"
    header.point_count = np.max(points.shape)
    header.scales = [0.0001, 0.0001, 0.0001]
    header.offsets = [round_up_to_nearest_100(info["min_x"]), round_up_to_nearest_100(info["min_y"]), round_up_to_nearest_100(info["min_z"])]
    header.mins = [info["min_x"], info["min_y"], info["min_z"]]
    header.maxs = [info["max_x"], info["max_y"], info["max_z"]]

    lasfile = laspy.LasData(header)

    # as simple as telling laspy which points go where
    lasfile.x = points[:,0]
    lasfile.y = points[:,1]
    lasfile.z = points[:,2]
    if rgb.size:
        lasfile.red = rgb[:,0]
        lasfile.green = rgb[:,1]
        lasfile.blue = rgb[:,2]

    lasfile.intensity = intensities
    # and write the file
    lasfile.write(output_file)

if __name__ == "__main__":
    parser = argparse.ArgumentParser(
                    prog='ProgramName',
                    description='What the program does',
                    epilog='Text at the bottom of help')
    parser.add_argument('input') 
    parser.add_argument('output')  
    args = parser.parse_args()
    # ply2las("../../../all.ply","../../../out.las")
    ply2las(args.input,args.output)
    