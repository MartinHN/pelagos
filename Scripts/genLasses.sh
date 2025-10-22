IF="/c/Users/User/Desktop/pelagos/downsampled/5/"
OF="/c/Users/User/Desktop/pelagos/indivLas/"
OUTPOTREE="/c/Users/User/Desktop/pelagos/mainPotree"

IF="/c/Users/User/Desktop/pelagos/downsampled/2/"
OF="/c/Users/User/Desktop/pelagos/indivLasBig/"
OUTPOTREE="/c/Users/User/Desktop/pelagos/mainPotreeBig"

CCE="/c/Program Files/CloudCompare/CloudCompare.exe"
OFBK="/c/Users/User/Desktop/pelagos/indivLasBk/"
CACHEF="/c/Users/User/Desktop/pelagos/indivCache/"
PY=/c/Users/User/Desktop/pelagos/unityprojects/3dcore/Scripts/ply2las.py 


read -r -p "Are you sure? [y/N] to delete $OF " response
case "$response" in
    [yY][eE][sS]|[yY]) 
        rm -r $OF
        mkdir $OF
        ;;
    *)
        exit 0
        ;;
esac





while read -r cloudpath transformfile; do 
cloudpath=$(cygpath -u "$cloudpath")
cloudpath=$IF$(basename $cloudpath)
transformfile=$(cygpath -u "$transformfile")
dest=$(basename $transformfile .txt)
dest=$OF$dest
cacheId=$({ printf '%s\0' "$OF$cloudpath"; cat "$transformfile"; } | sha256sum | awk '{print $1}')
if [[ -f "$CACHEF$cacheId" ]]; then
echo "getting from cache $dest"
cp $CACHEF$cacheId $dest.las
continue;
fi
echo $cacheId
echo $cloudpath $transformfile

"$CCE" -SILENT -C_EXPORT_FMT PLY -O -GLOBAL_SHIFT 0 0 0 "$cloudpath" -AUTO_SAVE OFF -APPLY_TRANS "$transformfile" -SAVE_CLOUDS FILE "$dest.ply"
python $PY "$dest.ply" "$dest.las"
rm "$dest.ply"
cp $dest.las "$CACHEF$cacheId"
 done < /c/Users/User/Desktop/pelagos/transforms/transforms.txt 
#  /c/Users/User/Desktop/pelagos/unityprojects/3dcore/LASmatrices/transformlist.txt

cd $OF
 PotreeConverter.exe -i $(ls *.las) -o $OUTPOTREE
# -GLOBAL_SHIFT AUTO 
#  -APPLY_TO_GLOBAL FORCE