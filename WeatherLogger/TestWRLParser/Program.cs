using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TestWRLParser {
   class Program {
      static void Main(string[] args) {
         string inputStr = "20:44 12/14/10 W   05MPH 460F 070F 037F 089% 30.14R 00.00\"D 00.00\"M 00.00\"T";
         string[] recordParts =
                  inputStr.Trim(" \n\r\t".ToCharArray()).Split(
                  " \n\r\t/:".ToCharArray(),
                  StringSplitOptions.RemoveEmptyEntries);
         DateTime curTime = new DateTime(
                  Convert.ToUInt16(recordParts[4]) + 2000,
                  Convert.ToUInt16(recordParts[2]),
                  Convert.ToUInt16(recordParts[3]),
                  Convert.ToUInt16(recordParts[0]),
                  Convert.ToUInt16(recordParts[1]),
                  0, 0);
         curTime = curTime.AddHours(9);
         Console.WriteLine("Time                : " + curTime.ToString());
         Console.WriteLine("Wind Direction      : " + 
                 (ushort)((ConvertCompassToDegrees)Enum.Parse(
                 typeof(ConvertCompassToDegrees), recordParts[5], true)));
         Console.WriteLine("Wind Speed          : " + 
                 Convert.ToSingle(recordParts[6].Trim(
                                  "MPH".ToCharArray())) / 1.15F);
         Console.WriteLine("Inside Temperature  : " + 
                 Convert.ToSingle(recordParts[8].Trim(
                                  "F".ToCharArray())));
         Console.WriteLine("Outside Temperature : " + 
                 Convert.ToSingle(recordParts[9].Trim(
                                  "F".ToCharArray())));
         Console.WriteLine("Humidity            : " + 
                 Convert.ToUInt16(recordParts[10].Trim(
                                  "%".ToCharArray())));
         Console.WriteLine("Pressure            : " + 
                 Convert.ToSingle(recordParts[11].Remove(
                 recordParts[11].Length - 1)));
         Console.WriteLine("Rain Fall           : " + 
                 Convert.ToSingle(recordParts[12].Trim(
                                  "\"DMT".ToCharArray())));
         Console.ReadKey(true);
      } // Main()

      #region ConvertCompassToDegrees Enumeration
      public enum ConvertCompassToDegrees : ushort
      {
         N = 0,
         NNE = 23,
         NE = 45,
         ENE = 68,
         E = 90,
         ESE = 113,
         SE = 135,
         SSE = 158,
         S = 180,
         SSW = 203,
         SW = 225,
         WSW = 248,
         W = 270,
         WNW = 293,
         NW = 315,
         NNW = 338
      } // enum ConvertCompassToDegrees
      #endregion
   } // class Program
} // namespace TestWRLParser
