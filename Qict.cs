/*************************************************************************

QICT: a pairwise test case generator
Author: James McCaffrey
This program originally appeared in the MSDN Magazine, December 2009
http://msdn.microsoft.com/en-us/magazine/ee819137.aspx
Shipped with no license and assumed to be public domain.

Modified by Sylvain Hallé <http://leduotang.ca/sylvain>
Université du Québec à Chicoutimi, May 2014

Under Windows:
To run this QICT code, launch a new instance of Visual Studio 2005 or 2008.
Create a new Console Application C# project.
Replace the VS-generated template code in file Program.cs with the Qict.cs
code in this download.
Copy file testData.txt into the root folder of your project.
Build and run by hitting the F5 key.

To compile (under Ubuntu):
mcs Qict.cs
mv Qict.exe qict
chmod +x qict

Usage:
qict [-c] [-h] <filename>
where filename is a text file containing the text data (see the article
or the sample file testData.txt included for more info)

*************************************************************************/
using System;
using System.Collections.Generic;

using System.IO;

namespace Gamma
{
  class Qict
  {
    static bool only_test_count = false;
    
    // Return codes
    static int ERR_OK = 0;
    static int ERR_NO_FILENAME = 1;
    static int ERR_EXCEPTION = 2;
    
    static int Main(string[] args) // {{{
    {
      string file = "";
      
      // Process command-line arguments
      ConsoleWriteLine("QICT: a pairwise test case generator\n(C) 2009 James McCaffrey, (C) 2014 Sylvain Hallé\n");
      if (args.Length <= 0)
      {
        ConsoleWriteLine("ERROR: no filename is specified");
        return ERR_NO_FILENAME;
      }
      
      for (int i = 0; i < args.Length; i++)
      {
        string arg = args[i];
        if (arg == "-c") // Show only test size, not actual tests
        {
          only_test_count = true;
        }
        else if (arg == "-h" || arg == "--help")
        {
          // Show usage
          ShowUsage();
          return ERR_OK;
        }
        else
        {
          file = arg;
        }
      }
      
      Random r = new Random(0);
      
      int numberParameters = 0;
      int numberParameterValues = 0;
      int numberPairs = 0;
      int poolSize = 20; // number of candidate testSet arrays to generate before picking one to add to testSets List
      
      int[][] legalValues = null; // in-memory representation of input file as ints
      string[] parameterValues = null; // one-dimensional array of all parameter values
      int[,] allPairsDisplay = null; // rectangular array; does not change, used to generate unusedCounts array
      List<int[]> unusedPairs = null; // changes
      int[,] unusedPairsSearch = null; // square array -- changes
      int[] parameterPositions = null; // the parameter position for a given value
      int[] unusedCounts = null; // count of each parameter value in unusedPairs List
      List<int[]> testSets = null; // the main result data structure
      
      try
      {
        // do a preliminary file read to determine number of parameters and number of parameter values
        FileStream fs = new FileStream(file, FileMode.Open);
        StreamReader sr = new StreamReader(fs);
        string line;
        while ((line = sr.ReadLine()) != null)
        {
          line = line.Trim();
          if (line == "" || line[0] == '#')
            continue;
          ++numberParameters;
          string[] lineTokens = line.Split(':');
          string[] strValues = lineTokens[1].Split(',');
          numberParameterValues += strValues.Length;
        }
        
        if (!only_test_count)
        {
          ConsoleWriteLine("- There are " + numberParameters + " parameters");
          ConsoleWriteLine("- There are " + numberParameterValues + " parameter values");
        }
        
        // now do a second file read to create the legalValues array, and the parameterValues array
        fs.Position = 0;
        
        legalValues = new int[numberParameters][];
        parameterValues = new string[numberParameterValues];
        int currRow = 0;
        int kk = 0; // points into parameterValues
        while ((line = sr.ReadLine()) != null)
        {
          line = line.Trim();
          if (line == "" || line[0] == '#')
            continue;
          string[] lineTokens = line.Split(':'); // separate parameter name from parameter values (as strings at this point)
          string[] strValues = lineTokens[1].Split(','); // pull out the individual parameter values into an array
          int[] values = new int[strValues.Length]; // create small row array for legalValues
          
          for (int i = 0; i < strValues.Length; ++i) // trim whitespace
          {
            strValues[i] = strValues[i].Trim();
            values[i] = kk;
            parameterValues[kk] = strValues[i];
            ++kk;
          }
          
          legalValues[currRow++] = values;
        } // while
        
        sr.Close();
        fs.Close();
        
        ConsoleWrite("- Parameter values:\n  ");
        for (int i = 0; i < parameterValues.Length; ++i)
          ConsoleWrite(parameterValues[i] + " ");
        ConsoleWriteLine("");
        
        ConsoleWriteLine("- Legal values internal representation: ");
        for (int i = 0; i < legalValues.Length; ++i)
        {
          ConsoleWrite("  * Parameter" + i + ": ");
          for (int j = 0; j < legalValues[i].Length; ++j)
          {
            ConsoleWrite(legalValues[i][j] + " ");
          }
          ConsoleWriteLine("");
        }
        
        // determine the number of pairs for this input set
        for (int i = 0; i <= legalValues.Length - 2; ++i)
        {
          for (int j = i + 1; j <= legalValues.Length - 1; ++j)
          {
            numberPairs += (legalValues[i].Length * legalValues[j].Length);
          }
        }
        ConsoleWriteLine("- There are " + numberPairs + " pairs ");
        
        // process the legalValues array to populate the allPairsDisplay & unusedPairs & unusedPairsSearch collections
        allPairsDisplay = new int[numberPairs, 2]; // rectangular array; does not change
        unusedPairs = new List<int[]>(); // List of pairs which have not yet been captured
        unusedPairsSearch = new int[numberParameterValues, numberParameterValues]; // square array -- changes
        
        int currPair = 0;
        for (int i = 0; i <= legalValues.Length - 2; ++i)
        {
          for (int j = i + 1; j <= legalValues.Length - 1; ++j)
          {
            int[] firstRow = legalValues[i];
            int[] secondRow = legalValues[j];
            for (int x = 0; x < firstRow.Length; ++x)
            {
              for (int y = 0; y < secondRow.Length; ++y)
              {
                allPairsDisplay[currPair, 0] = firstRow[x]; // pair first value
                allPairsDisplay[currPair, 1] = secondRow[y]; // pair second value
                
                int[] aPair = new int[2];
                aPair[0] = firstRow[x];
                aPair[1] = secondRow[y];
                unusedPairs.Add(aPair);
                
                unusedPairsSearch[firstRow[x], secondRow[y]] = 1;
                
                ++currPair;
              } // y
            } // x
          } // j
        } // i
        
        //ConsoleWriteLine("allPairsDisplay array:");
        //for (int i = 0; i < numberPairs; ++i)
        //{
        //  ConsoleWriteLine(i + " " + allPairsDisplay[i, 0] + " " + allPairsDisplay[i, 1]);
        //}
        
        //ConsoleWriteLine("unusedPairs array:");
        //for (int i = 0; i < numberPairs; ++i)
        //{
        //  if (unusedPairs[i] != null)
        //  {
        //    ConsoleWriteLine(i + " " + unusedPairs[i][0] + " " + unusedPairs[i][1]);
        //  }
        //}
        
        //ConsoleWriteLine("unusedPairs List<>:");
        //for (int i = 0; i < unusedPairs.Count; ++i)
        //{
        //  int[] curr = unusedPairs[i];
        //  ConsoleWriteLine(i + " " + curr[0] + " " + curr[1]);
        //}
        
        //ConsoleWriteLine("allPairsSearch array:");
        //for (int row = 0; row < numberParameterValues; ++row)
        //{
        //  for (int col = 0; col < numberParameterValues; ++col)
        //  {
        //    ConsoleWrite(allPairsSearch[row, col] + " ");
        //  }
        //  ConsoleWriteLine("");
        //}
        
        // process legalValues to populate parameterPositions array
        parameterPositions = new int[numberParameterValues]; // the indexes are parameter values, the cell values are positions within a testSet
        int k = 0;  // points into parameterPositions
        for (int i = 0; i < legalValues.Length; ++i)
        {
          int[] curr = legalValues[i];
          for (int j = 0; j < curr.Length; ++j)
          {
            parameterPositions[k++] = i;
          }
        }
        //ConsoleWriteLine("parameterPositions:");
        //for (int i = 0; i < parameterPositions.Length; ++i)
        //{
        //  ConsoleWrite(parameterPositions[i] + " ");
        //}
        //ConsoleWriteLine("");
        
        
        // process allPairsDisplay to determine unusedCounts array
        unusedCounts = new int[numberParameterValues];  // inexes are parameter values, cell values are counts of how many times the parameter value apperas in the unusedPairs collection
        for (int i = 0; i < allPairsDisplay.GetLength(0); ++i)
        {
          ++unusedCounts[allPairsDisplay[i, 0]];
          ++unusedCounts[allPairsDisplay[i, 1]];
        }
        
        //ConsoleWriteLine("unusedCounts: ");
        //for (int i = 0; i < unusedCounts.Length; ++i)
        //{
        //  ConsoleWrite(unusedCounts[i] + " ");
        //}
        //ConsoleWriteLine("");
        
        //==============================================================================================================
        testSets = new List<int[]>();  // primary data structure
        ConsoleWriteLine("\nComputing testsets which capture all possible pairs...");
        while (unusedPairs.Count > 0) // as long as ther are unused pairs to account for . . .
        {
          int[][] candidateSets = new int[poolSize][]; // holds candidate testSets
          
          for (int candidate = 0; candidate < poolSize; ++candidate)
          {
            int[] testSet = new int[numberParameters]; // make an empty candidate testSet
            
            // pick "best" unusedPair -- the pair which has the sum of the most unused values
            int bestWeight = 0;
            int indexOfBestPair = 0;
            for (int i = 0; i < unusedPairs.Count; ++i)
            {
              int[] curr = unusedPairs[i];
              int weight = unusedCounts[curr[0]] + unusedCounts[curr[1]];
              if (weight > bestWeight)
              {
                bestWeight = weight;
                indexOfBestPair = i;
              }
            }
            
            //// pick best unusedPair, starting at a random index -- does not seem to help any
            //int bestWeight = 0;
            //int indexOfBestPair = 0;
            //int currIndex = r.Next(unusedPairs.Count);
            //for (int ct = 0; ct < unusedPairs.Count; ++ct) // count is predetermine
            //{
            //  if (currIndex == unusedPairs.Count - 1) // if at end of unusedPairs, jump to beginnng
            //  {
            //    currIndex = 0;
            //  }
            //  int[] curr = unusedPairs[currIndex];
            //  int weight = unusedCounts[curr[0]] + unusedCounts[curr[1]];
            //  if (weight > bestWeight)
            //  {
            //    bestWeight = weight;
            //    indexOfBestPair = currIndex;
            //  }
            //  ++currIndex;
            //}
            
            int[] best = new int[2]; // a copy is not strictly necesary here
            unusedPairs[indexOfBestPair].CopyTo(best, 0);
            
            //ConsoleWriteLine("Best pair is " + best[0] + ", " + best[1] + " at " + indexOfBestPair + " with weight " + bestWeight);
            
            int firstPos = parameterPositions[best[0]]; // position of first value from best unused pair
            int secondPos = parameterPositions[best[1]];
            
            //ConsoleWriteLine("The best pair belongs at positions " + firstPos + " and " + secondPos);
            
            // generate a random order to fill parameter positions
            int[] ordering = new int[numberParameters];
            for (int i = 0; i < numberParameters; ++i) // initially all in order
              ordering[i] = i;
            
            // put firstPos at ordering[0] && secondPos at ordering[1]
            ordering[0] = firstPos;
            ordering[firstPos] = 0;
            
            int t = ordering[1];
            ordering[1] = secondPos;
            ordering[secondPos] = t;
            
            // shuffle ordering[2] thru ordering[last]
            for (int i = 2; i < ordering.Length; i++)  // Knuth shuffle. start at i=2 because want first two slots left alone
            {
              int j = r.Next(i, ordering.Length);
              int temp = ordering[j];
              ordering[j] = ordering[i];
              ordering[i] = temp;
            }
            
            //ConsoleWriteLine("Order: ");
            //for (int i = 0; i < ordering.Length; ++i)
            //  ConsoleWrite(ordering[i] + " ");
            //ConsoleWriteLine("");
            //Console.ReadLine();
            
            // place two parameter values from best unused pair into candidate testSet
            testSet[firstPos] = best[0];
            testSet[secondPos] = best[1];
            //ConsoleWriteLine("Placed params " + best[0] + " " + best[1] + " at " + firstPos + " and " + secondPos);
            //Console.ReadLine();
            
            // for remaining parameter positions in candidate testSet, try each possible legal value, picking the one which captures the most unused pairs . . .
            for (int i = 2; i < numberParameters; ++i) // start at 2 because first two parameter have been placed
            {
              int currPos = ordering[i];
              int[] possibleValues = legalValues[currPos];
              //ConsoleWriteLine("possibles are ");
              //for (int z = 0; z < possibleValues.Length; ++z)
              //  ConsoleWriteLine(possibleValues[z]);
              //ConsoleWriteLine("");
              
              int currentCount = 0;  // count the unusedPairs grabbed by adding a possible value
              int highestCount = 0;  // highest of these counts
              int bestJ = 0;         // index of the possible value which yields the highestCount
              for (int j = 0; j < possibleValues.Length; ++j) // examine pairs created by each possible value and each parameter value already there
              {
                currentCount = 0;
                for (int p = 0; p < i; ++p)  // parameters already placed
                {
                  int[] candidatePair = new int[] { possibleValues[j], testSet[ordering[p]] };
                  //ConsoleWriteLine("Considering pair " + possibleValues[j] + ", " + testSet[ordering[p]]);
                  
                  if (unusedPairsSearch[candidatePair[0], candidatePair[1]] == 1 ||
                    unusedPairsSearch[candidatePair[1], candidatePair[0]] == 1)  // because of the random order of positions, must check both possibilities
                  {
                    //ConsoleWriteLine("Found " + candidatePair[0] + "," + candidatePair[1] + " in unusedPairs");
                    ++currentCount;
                  }
                  else
                  {
                    //ConsoleWriteLine("Did NOT find " + candidatePair[0] + "," + candidatePair[1] + " in unusedPairs");
                  }
                } // p -- each previously placed paramter
                if (currentCount > highestCount)
                {
                  highestCount = currentCount;
                  bestJ = j;
                }
                
              } // j -- each possible value at currPos
              //ConsoleWriteLine("The best value is " + possibleValues[bestJ] + " with count = " + highestCount);
              
              testSet[currPos] = possibleValues[bestJ]; // place the value which captured the most pairs
            } // i -- each testSet position 
            
            //=========
            //ConsoleWriteLine("\n============================");
            //ConsoleWriteLine("Adding candidate testSet to candidateSets array: ");
            //for (int i = 0; i < numberParameters; ++i)
            //  ConsoleWrite(testSet[i] + " ");
            //ConsoleWriteLine("");
            //ConsoleWriteLine("============================\n");
            
            candidateSets[candidate] = testSet;  // add candidate testSet to candidateSets array
          } // for each candidate testSet
          
          //ConsoleWriteLine("Candidates testSets are: ");
          //for (int i = 0; i < candidateSets.Length; ++i)
          //{
          //  int[] curr = candidateSets[i];
          //  ConsoleWrite(i + ": ");
          //  for (int j = 0; j < curr.Length; ++j)
          //  {
          //    ConsoleWrite(curr[j] + " ");
          //  }
          //  ConsoleWriteLine(" -- captures " + NumberPairsCaptured(curr, unusedPairsSearch));
          //}
          
          // Iterate through candidateSets to determine the best candidate
          
          int indexOfBestCandidate = r.Next(candidateSets.Length); // pick a random index as best
          int mostPairsCaptured = NumberPairsCaptured(candidateSets[indexOfBestCandidate], unusedPairsSearch);
          
          int[] bestTestSet = new int[numberParameters];
          
          for (int i = 0; i < candidateSets.Length; ++i)
          {
            int pairsCaptured = NumberPairsCaptured(candidateSets[i], unusedPairsSearch);
            if (pairsCaptured > mostPairsCaptured)
            {
              mostPairsCaptured = pairsCaptured;
              indexOfBestCandidate = i;
            }
            //ConsoleWriteLine("Candidate " + i + " captured " + mostPairsCaptured);
          }
          //ConsoleWriteLine("Candidate number " + indexOfBestCandidate + " is best");
          candidateSets[indexOfBestCandidate].CopyTo(bestTestSet, 0);
          testSets.Add(bestTestSet); // Add the best candidate to the main testSets List
          
          // now perform all updates
          
          //ConsoleWriteLine("Updating unusedPairs");
          //ConsoleWriteLine("Updating unusedCounts");
          //ConsoleWriteLine("Updating unusedPairsSearch");
          for (int i = 0; i <= numberParameters - 2; ++i)
          {
            for (int j = i + 1; j <= numberParameters - 1; ++j)
            {
              int v1 = bestTestSet[i]; // value 1 of newly added pair
              int v2 = bestTestSet[j]; // value 2 of newly added pair
              
              //ConsoleWriteLine("Decrementing the unused counts for " + v1 + " and " + v2);
              --unusedCounts[v1];
              --unusedCounts[v2];
              
              //ConsoleWriteLine("Setting unusedPairsSearch at " + v1 + " , " + v2 + " to 0");
              unusedPairsSearch[v1, v2] = 0;
              
              for (int p = 0; p < unusedPairs.Count; ++p)
              {
                int[] curr = unusedPairs[p];
                
                if (curr[0] == v1 && curr[1] == v2)
                {
                  //ConsoleWriteLine("Removing " + v1 + ", " + v2 + " from unusedPairs List");
                  unusedPairs.RemoveAt(p);
                }
              }
            } // j
          } // i
          
        } // primary while loop
        
        // Display results
        int num_results = testSets.Count;
        if (only_test_count)
        {
          Console.WriteLine("There are " + num_results + " test cases");
          return ERR_OK;
        }
        
        ConsoleWriteLine("\nResult test sets: \n");
        for (int i = 0; i < num_results; ++i)
        {
          ConsoleWrite(i.ToString().PadLeft(3) + "\t");
          int[] curr = testSets[i];
          for (int j = 0; j < numberParameters; ++j)
          {
            ConsoleWrite(parameterValues[curr[j]] + "\t");
          }
          ConsoleWriteLine("");
        }
        ConsoleWriteLine("\nEnd\n");
        //Console.ReadLine();
      }
      catch (Exception ex)
      {
        Console.WriteLine("Fatal: " + ex.Message);
        //Console.ReadLine();
        return ERR_EXCEPTION;
      }
      return ERR_OK;
    } // Main() }}}
    
    static int NumberPairsCaptured(int[] ts, int[,] unusedPairsSearch)  // number of unused pairs captured by testSet ts {{{
    {
      int ans = 0;
      for (int i = 0; i <= ts.Length - 2; ++i)
      {
        for (int j = i + 1; j <= ts.Length - 1; ++j)
        {
          if (unusedPairsSearch[ts[i], ts[j]] == 1)
            ++ans;
        }
      }
      return ans;
      } // NumberPairsCaptured() }}}
    
    static void ShowUsage() // {{{
    {
      Console.WriteLine("Usage: qict [-c] [-h] filename");
      Console.WriteLine("  -c     Show only number of tests computed");
      Console.WriteLine("  -h     Show command-line usage\n");
    } // ShowUsage() }}}
    
    static void ConsoleWrite(string msg) // {{{
    {
      if (!only_test_count)
        Console.Write(msg);
    } // ConsoleWrite() }}}
    
    static void ConsoleWriteLine(string msg) // {{{
    {
      ConsoleWrite(msg + "\n");
    } // ConsoleWrite() }}}
    
  } // class
} // ns
// :folding=explicit:wrap=none:
