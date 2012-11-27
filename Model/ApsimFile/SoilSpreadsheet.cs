
using System;
using System.Collections;
using System.Collections.Specialized;
using System.Data;
using System.IO;
//using System.Windows.Forms;
using System.Xml;
using ApsimFile;
using CSGeneral;
#if __WIN32__
using ExcelUtility;
#endif
using System.Collections.Generic;


namespace ApsimFile
   {
   public class SoilSpreadsheet
      {
      public delegate void ProgressNotifier(int Percent);


      private static string[] VarNames = {"Name", "Country", "State", "Region", "NearestTown", "Site", 
                                  "ApsoilNumber", "SoilType", "Latitude", "Longitude",
                                  "LocationAccuracy", "DataSource", "Comments", "NaturalVegetation",
                                  "MunsellColour", //"MunsellColourCode", 
                                  "Thickness (mm)", 
                                  "BD (g/cc)", //"BDCode",
                                  "Rocks (%)", //"RocksCode",
                                  "Texture", //"TextureCode",
                                  "SAT (mm/mm)", //"SATCode",
                                  "DUL (mm/mm)", //"DULCode", 
                                  "LL15 (mm/mm)", //"LL15Code",
                                  "AirDry (mm/mm)", //"AirDryCode",
                                  "OC (Walkley Black %)", //"OCCode", 
                                  "EC (1:5 dS/m)", //"ECCode", 
                                  "PH (1:5 water)", //"PHCode", 
                                  "CL (mg/kg)", //"CLCode", 
                                  "Boron (mg/kg)", //"BoronCode", 
                                  "CEC (cmol+/kg)", //"CECCode", 
                                  "Ca (cmol+/kg)", //"CaCode",
                                  "Mg (cmol+/kg)", //"MgCode",
                                  "Na (cmol+/kg)", //"NaCode",
                                  "K (cmol+/kg)", //"KCode",
                                  "ESP (%)", //"ESPCode",
                                  "Mn (mg/kg)", //"MnCode",
                                  "Al (cmol+/kg)", //"AlCode",
                                  "ParticleSizeSand (%)", //"ParticleSizeSandCode",
                                  "ParticleSizeSilt (%)", //"ParticleSizeSiltCode",
                                  "ParticleSizeClay (%)"}; //"ParticleSizeClayCode"};


      public SoilSpreadsheet()
         {
         }

#if __WIN32__
      static public string OpenXLS(string FileName, ProgressNotifier Notifier)
         {
         // --------------------------------------------------------------
         // Read through all rows and columns in the specified XLS file
         // and create a temporaty .soils file with all the 
         // soils found in the spreadsheet. Return the temporary file to caller
         // --------------------------------------------------------------
         
         DataTable Table = ExcelHelper.GetDataFromSheet(FileName, "SoilData");

         // remove DepthMidPoints and Depth columns if they exist.


         XmlDocument Doc = new XmlDocument();
         XmlNode AllSoils = Doc.CreateElement("folder");
         Doc.AppendChild(AllSoils);
         XmlHelper.SetName(AllSoils, "AllSoils");
         XmlHelper.SetAttribute(AllSoils, "version", APSIMChangeTool.CurrentVersion.ToString());
         
         // Loop through all blocks of rows in datatable from XLS, create a
         // soil and store soil in correct location in the AllSoils XML.
         int Row = 0;
         int counter = 0;
         Soil NewSoil = Soil.Create("Dummy");
         while (Row < Table.Rows.Count)
            {
            XmlNode NewSoilNode = AllSoils.AppendChild(Doc.CreateElement("soil"));

            NewSoil.UseNode(NewSoilNode);
            NewSoil.Read(Table, Row);

            // Make sure the DataSource and Comment nodes under the Water node are multiedits.
            XmlNode DataSourceNode = XmlHelper.Find(NewSoilNode, "DataSource");
            if (DataSourceNode != null)
               XmlHelper.SetAttribute(DataSourceNode, "type", "multiedit");
            XmlNode CommentNode = XmlHelper.Find(NewSoilNode, "Comments");
            if (CommentNode != null)
               XmlHelper.SetAttribute(CommentNode, "type", "multiedit");

            AddSoilToXML(NewSoil, AllSoils, NewSoilNode);


            Row += NewSoil.Variable("Thickness (mm)").Length;

            if (Notifier != null)
               Notifier.Invoke(Convert.ToInt32(Row * 100.0 / Table.Rows.Count));
            counter++;
            if (counter == 50)
               {
               GC.Collect();
               counter = 0;
               }

            }
         string TempFileName = Path.GetTempFileName();
         Doc.Save(TempFileName);
         return TempFileName;
         }
#endif		

      /// <summary>
      /// Add the specified soil the the AllSoils node.
      /// </summary>
      private static void AddSoilToXML(Soil NewSoil, XmlNode AllSoils, XmlNode NewSoilNode)
         {
         string SoilPath = CalcPathFromSoil(NewSoil);
         XmlNode ParentNode;
         if (SoilPath == "")
            ParentNode = AllSoils;
         else
            ParentNode = EnsureNodeExists(AllSoils, SoilPath);

         ParentNode.AppendChild(NewSoilNode);
         }

      /// <summary>
      /// Calculate a path for the specified soil.
      /// </summary>
      /// <param name="NewSoil"></param>
      /// <returns></returns>
      private static string CalcPathFromSoil(Soil NewSoil)
         {
         string Country = NewSoil.Property("Country");
         string State = NewSoil.Property("State");
         string Region = NewSoil.Property("Region");
         if (Country == "")
            return "";
         else
            {
            string Path = Country;
            if (State != "")
               Path += "/" + State;
            else
               return Path;
            if (Region != "")
               Path += "/" + Region;
            return Path;
            }
         }

      /// <summary>
      /// Ensure a node exists by creating nodes as necessary
      /// for the specified node path.
      /// </summary>
      public static XmlNode EnsureNodeExists(XmlNode Node, string NodePath)
         {
         if (NodePath.Length == 0)
            return Node;

         int PosDelimiter = NodePath.IndexOf(XmlHelper.Delimiter);
         string ChildNameToMatch = NodePath;
         if (PosDelimiter != -1)
            ChildNameToMatch = NodePath.Substring(0, PosDelimiter);

         foreach (XmlNode Child in Node.ChildNodes)
            {
            if (XmlHelper.Name(Child).ToLower() == ChildNameToMatch.ToLower())
               {
               if (PosDelimiter == -1)
                  return Child;
               else
                  return EnsureNodeExists(Child, NodePath.Substring(PosDelimiter + 1));
               }
            }

         // Didn't find the child node so add one and continue.
         XmlNode NewChild = Node.AppendChild(Node.OwnerDocument.CreateElement("Folder"));
         XmlHelper.SetName(NewChild, ChildNameToMatch);

         if (PosDelimiter == -1)
            return NewChild;
         else
            return EnsureNodeExists(NewChild, NodePath.Substring(PosDelimiter + 1));
         }

#if __WIN32__		
      static public void Export(string FileName, XmlNode Xml, StringCollection XMLPaths)
         {
         // --------------------------------------------------------------
         // Export the specific XML nodes (as specified by XMLPaths) from
         // the specified Xml to the given XLS filename.
         // --------------------------------------------------------------

         File.Delete(FileName);
         DataTable Table = new DataTable("SoilData");
         int Row = 0;
         foreach (string SelectedPath in XMLPaths)
            {
            XmlNode SelectedNode = XmlHelper.Find(Xml, SelectedPath);
            if (SelectedNode != null)
               CreateTableFromData(SelectedNode, Table, SelectedPath, ref Row);
            }
         ExcelHelper.SendDataToSheet(FileName, "SoilData", Table);
         }
#endif
		
      static private void CreateTableFromData(XmlNode Data, DataTable Table, string ChildPath, ref int Row)
         {
         // --------------------------------------------------------------
         // Look at the Data node passed in. If it's a <soil> node then
         // create a table with all the soil data. If it's a <soils> or
         // a <folder> node then recursively go down looking for soil nodes.
         // The end result is a DataTable populated with data for
         // all soil nodes found.
         // --------------------------------------------------------------

         if (XmlHelper.Type(Data).ToLower() == "soil")
            {
            Soil NewSoil = Soil.CreateFromXML(Data.OuterXml);
            List<string> VariableNames = new List<string>();
            VariableNames.AddRange(VarNames);
            NewSoil.Write(Table, VariableNames);
            }

         foreach (XmlNode Child in XmlHelper.ChildNodes(Data, ""))
            {
            if (XmlHelper.Type(Child).ToLower() == "soil" ||
                XmlHelper.Type(Child).ToLower() == "soils" || XmlHelper.Type(Child).ToLower() == "folder")
               CreateTableFromData(Child, Table, ChildPath + "/" + XmlHelper.Name(Child), ref Row); // recursion
            }
         }
      }
   }