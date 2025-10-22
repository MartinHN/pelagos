import hou
import os
import shutil


# CONFIG
out_dir = "C:/Users/User/Desktop/pelagos/transforms/"         # output folder (must exist or will be created)
base_index = 0                         # starting index
node_filter = lambda n: n.type().name() == "geo"  # change filter as needed
file_sop_name = "file1"                # name of File SOP inside each geo that holds the .ply path
file_sop_parm = "file"                 # parameter name on File SOP that stores the path

# Ensure output directory exists
if os.path.exists(out_dir):
        shutil.rmtree(out_dir)
os.makedirs(out_dir)


root = hou.node("/obj")
nodes = [n for n in root.children() if node_filter(n)]
outlines = []
index = base_index
for n in nodes:
    # try find the File SOP to get path; otherwise optionally use node name
    ply_path = None
    file_sop = None
    try:
        file_sop = n.children()[0]
    except Exception:
        file_sop = None

    if file_sop and file_sop.parm(file_sop_parm):
        try:
            ply_path = file_sop.parm(file_sop_parm).eval()
        except Exception:
            ply_path = None

    if not ply_path:
            print("Skipping", n.path(), "- no File SOP path found")
            continue
    else:
        # get base name without extension
        base = os.path.splitext(os.path.basename(ply_path))[0]
        if not base:
            base = n.name()

    # build output filename: BASE_index.txt (compatible with CloudCompare)
    out_name = "{}_{}.txt".format(base, index)
    out_path = os.path.join(out_dir, out_name)

    # get world transform matrix as tuple (row-major)
    wm = n.worldTransform()              # hou.Matrix4
    vals = list(wm.asTuple())            # 16 floats in row-major order

    # CloudCompare expects 4 rows of 4 numbers. We'll write row-major:
    # m00 m01 m02 m03
    # m10 m11 m12 m13
    # m20 m21 m22 m23
    # m30 m31 m32 m33
    with open(out_path, "w") as f:
        for r in range(4):
            # row_vals = vals[r*4:(r+1)*4]
            row_vals = [vals[r], vals[r+4], vals[r+8], vals[r+12]]
            f.write(" ".join(str(float(v)) for v in row_vals))
            f.write("\n")

    print("Wrote:", out_path)
    outlines.append(ply_path+" "+out_path+"\n")
    index += 1

# print(outlines)
with open(out_dir+"transforms.txt", "w") as f:
    f.writelines(outlines)
print("Done. Wrote {} files starting at index {}".format(index - base_index, base_index))
