IF="/c/Users/User/Desktop/pelagos/downsampled/"
cd $IF
FOL=2/
ALLF=$(ls $FOL*.las)


OF="/c/Users/User/Desktop/pelagos/potree/$FOL/"
for F in $ALLF ; do
if echo "$F" | grep -q '.ply'; then
BN="$(basename $F .ply)"
else
BN="$(basename $F .las)"
fi
PotreeConverter.exe    -i "$IF$F" -o "$OF$BN"
done;
