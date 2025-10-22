CCE="/c/Program Files/CloudCompare/CloudCompare.exe"
IF="/c/Users/User/Desktop/pelagos/originals_PELAGOS/"
OF="/c/Users/User/Desktop/pelagos/downsampled/"


ALLF="IsabelUrbina/digue_IUB.las
IsabelUrbina/reefscape-NC_IUB.las
Semantic/Acropodes.las
Semantic/Arene.las
Semantic/Bloc.las
Semantic/Emissaire.las
Semantic/Portissol.las
Semantic/Voitures.las
IVMTechnologie/3PP_Cave_Dense_cloud.ply
IVMTechnologie/Vobster_Aircraft_Dense_cloud.ply
Septentrion/Cap_20250514.ply
Septentrion/Liban_2021.ply
Septentrion/Messerschmitt_2022.ply
Septentrion/TISCO_BAN_CAB_T3_20230719.ply
Septentrion/Transect_1_20180428.ply
Septentrion/Transect_28_20180507.ply
geolab/Canons.laz
geolab/Colonie1.laz
geolab/Colonie2.laz
geolab/Colonie3.laz
geolab/Recif1.laz
geolab/Epave_x3.las"

ALLF=geolab/Epave_x3.las


#IsabelUrbina/reefscape-NC-HQ_IUB.las
# IsabelUrbina/corail_branchu_NouvelleCalenodie.las
# IsabelUrbina/corail_foliace_NouvelleCalenodie.las
# IsabelUrbina/corail_tabulaire_NouvelleCalenodie.las
# IsabelUrbina/corail_tabulaire_NouvelleCalenodie2.las
# IVMTechnologie/INPP_Quay_Dense_cloud.ply

# -EXTRACT_VERTICES -AUTO_SAVE OFF -SS SPATIAL 0.0005 -DROP_GLOBAL_SHIFT
FN=""
for F in $ALLF ; do

if echo "$F" | grep -q 'Septentrion'; then
VCMD=-EXTRACT_VERTICES
else
VCMD=""
fi

PREFX=2/
if echo "$F" | grep -q '.ply'; then
BN="$PREFX$(basename $F .ply)"
else
BN="$PREFX$(basename $F .las)"
fi
PLYF="$OF$BN.ply"
echo $BN
"$CCE" -SILENT -C_EXPORT_FMT PLY -O -GLOBAL_SHIFT AUTO simpleCenter.ply -O -GLOBAL_SHIFT AUTO "$IF$F" -AUTO_SAVE OFF -MATCH_CENTERS -AUTO_SAVE OFF $VCMD -AUTO_SAVE OFF -SS SPATIAL 0.002 -AUTO_SAVE OFF -DROP_GLOBAL_SHIFT -SELECT_ENTITIES -LAST 1 -SAVE_CLOUDS FILE "$PLYF"
#"$CCE" -SILENT -C_EXPORT_FMT LAS -O -GLOBAL_SHIFT AUTO "$PLYF" -SAVE_CLOUDS FILE "${OF}$BN.las" 
# rm "$PLYTMP"

done;