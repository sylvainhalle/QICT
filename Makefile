PHONY : build clean

build : qict

qict: Qict.cs
	mcs Qict.cs -out:qict

clean :
	-\rm qict
