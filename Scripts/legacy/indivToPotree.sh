set +x
set +e

# ALLF="05_Portissol.las_0.mat.las  05_Portissol.las_1.mat.las"
cd /c/Users/User/Desktop/pelagos/indivLas/
ALLF=$(ls)
pwd

 PotreeConverter.exe -i $ALLF -o "/c/Users/User/Desktop/pelagos/mainPotree"

