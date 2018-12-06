# QICT: A Pairwise Test Case Generator

This program originally appeared in the [MSDN Magazine, December
2009](http://msdn.microsoft.com/en-us/magazine/ee819137.aspx).

I took the code there (assumed to be public domain) and polished it a bit
to add command-line parameters and a few options. The bulk of the algorithm
remains unchanged.

## How to compile

First, clone the repository.

### Under Windows
To run this QICT code, launch a new instance of Visual Studio 2005 or 2008.
Create a new Console Application C# project.
Replace the VS-generated template code in file Program.cs with the Qict.cs
code in this download.
Copy all .txt into the root folder of your project.
Build and run by hitting the F5 key.

### Under Linux
```
mcs Qict.cs -out:qict
```
(or use `make build`)

NB: mcs is the Mono C# compiler

##Usage:
```
qict [-c] [-h] <filename>  [-i] <invalidCombinaisonsFilename>
```
where filename is a text file containing the text data (see the article
or the sample file testData.txt included for more info) and invalidCombinaisonsFilename
is the file containning invalids combinaisons to exclude during the test

the output will display something like this: (in the case below we didn't use forbdiden combinaisons)
```bash
QICT: a pairwise test case generator
(C) 2009 James McCaffrey, (C) 2014 Sylvain Hallé

- There are 4 parameters
- There are 11 parameter values
- Parameter values:
  a b c d e f g h i j k 
- Legal values internal representation: 
  * Parameter0: 0 1 
  * Parameter1: 2 3 4 5 
  * Parameter2: 6 7 8 
  * Parameter3: 9 10 
- There are 44 pairs 

Computing testsets which capture all possible pairs...

Result test sets: 

  0		a	c	g	j	
  1		b	d	g	k	
  2		a	e	h	k	
  3		b	f	i	j	
  4		b	c	h	j	
  5		a	d	i	j	
  6		a	f	g	k	
  7		b	e	i	j	
  8		a	d	h	j	
  9		a	c	i	k	
 10		a	e	g	j	
 11		a	f	h	j	

End

``` 
