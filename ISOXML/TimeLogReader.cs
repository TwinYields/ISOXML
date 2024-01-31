/*
 *  This file is part of ISOXML
 *
 *  Copyright 2022 Juha Backman & Matti Pastell / Natural Resources Institute Finland
 *
 *  ISOXML is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU Lesser General Public License as
 *  published by the Free Software Foundation, either version 3 of
 *  the License, or (at your option) any later version.
 *
 *  ISOXML is distributed in the hope that it will be useful, but
 *  WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 *  GNU Lesser General Public License for more details.
 *
 *  You should have received a copy of the GNU Lesser General Public
 *  License along with ISOXML.
 *  If not, see <http://www.gnu.org/licenses/>.
 */


using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;
using System.Linq;
using System.Globalization;

namespace ISOXML
{

    public class LogElement
    {
        public string name;
        public string DPDdesignator;
        public string DVCdesignator;
        public string DETdesignator;
        public string DETno;
        public int DDI;
        public Type type;

        public virtual string getValueString(int index)
        {
            return "";
        }

        public virtual int getSize()
        {
            return 0;
        }
    }

    public class LogElementType<T> : LogElement
    {
        public List<T> values;

        public LogElementType(string description)
        {
            name = description;
            DPDdesignator = "";
            DVCdesignator = "";
            DETdesignator = "";
            DETno = "";
            DDI = -1;
            values = new List<T>();
            type = typeof(T);
        }

        public LogElementType(string dpd, string dvc, string det, string detno, string ddi)
        {
            name = dpd;
            DPDdesignator = dpd;
            DVCdesignator = dvc;
            DETdesignator = det;
            DETno = detno;
            DDI = Convert.ToInt32(ddi, 16);
            values = new List<T>();
            type = typeof(T);
        }

        public override string getValueString(int index)
        {
            return values.ElementAt(index).ToString();
        }

        public override int getSize()
        {
            return values.Count;
        }
    }

    public class TimeLogData
    {
        public string taskname;
        public string field;
        public string farm;
        public Dictionary<string, string> products;
        public List<Dictionary<string, string>> devices;

        public List<LogElement> datalogheader = new List<LogElement>();
        public List<LogElement> datalogdata = new List<LogElement>();
    }
    public static class TimeLogReader
    {
        //Find correct file in case sensitive file systems
        static string FindFile(string directory, string name)
        {
            return Directory.GetFiles(directory).Where(x => x.ToLower().EndsWith(name.ToLower())).Single();
        }

        public static List<TimeLogData> ReadTaskFile(String filename)
        {
            // Read main file and linked XML files

            XDocument ISOTaskFile;
            try
            {
                ISOTaskFile = XDocument.Load(filename);
            }
            catch (System.IO.FileNotFoundException e)
            {
                Console.WriteLine(e.Message);
                return new List<TimeLogData>();
            }


            var XFRlist = ISOTaskFile.Root.Descendants("XFR");
            string directory = System.IO.Path.GetDirectoryName(filename);

            foreach (var XFR in XFRlist)
            {
                XDocument EXTFile;
                try
                {
                    string file = FindFile(directory, XFR.Attribute("A").Value + ".xml");
                    EXTFile = XDocument.Load(file);
                }
                catch (System.IO.FileNotFoundException e)
                {
                    Console.WriteLine(e.Message);
                    return new List<TimeLogData>();
                }

                ISOTaskFile.Root.Add(EXTFile.Element("XFC").Elements());
            }

            ISOTaskFile.Root.Descendants("XFR").Remove();

            List<TimeLogData> TLGList = new List<TimeLogData>();

            Dictionary<string, string> productPDTs = ISOTaskFile.Root.Descendants("PDT").ToDictionary(
                attr => attr.Attribute("A").Value, attr => attr.Attribute("B").Value);
            var TZN = ISOTaskFile.Root.Descendants("TZN");
            Dictionary<string, string> products = productPDTs;

            // Try to link product names with DETs and fall back to using PDT name if
            // it fails (some tested task files are missing PDV attribute D).
            if (TZN.Count() > 0 && products.Count >0)
            {
                try
                {
                    products = TZN.First().Descendants("PDV").ToDictionary(attr => attr.Attribute("D").Value,
                        attr => productPDTs[attr.Attribute("C").Value]);
                }
                catch (System.NullReferenceException)
                {
                    //Console.WriteLine("PDT exception");
                }
            }

            foreach (var TSK in ISOTaskFile.Root.Descendants("TSK"))
            {
                // Read XML-files of planned task
                if (TSK.Attribute("G").Value == "1")
                {
                    TimeLogData TLGdata = new TimeLogData();

                    if (TSK.Attribute("B") != null) {
                        TLGdata.taskname = TSK.Attribute("B").Value;
                    }
                    else
                    {
                        TLGdata.taskname = "";
                    }


                    try {
                        TLGdata.field = ISOTaskFile.Root.Descendants("PFD").Where(pdf => pdf.Attribute("A").Value == TSK.Attribute("E").Value).Single().Attribute("C").Value;
                    }
                    catch {
                        TLGdata.field = "";
                    }

                    TLGdata.products = products;

                    if (ISOTaskFile.Root.Descendants("FRM").Count() > 0)
                    {
                        TLGdata.farm = ISOTaskFile.Root.Descendants("FRM").Where(frm => frm.Attribute("A").Value == TSK.Attribute("D").Value).Single().Attribute("B").Value;
                    }

                    TLGList.Add(TLGdata);
                }

                else
                {
                    // Read binary timelog files of implemented task
                foreach (var TLG in TSK.Descendants("TLG"))
                {
                    TimeLogData TLGdata = new TimeLogData();

                    try {
                        TLGdata.taskname = TSK.Attribute("B").Value;
                    }
                    catch
                    {
                        TLGdata.taskname = "";
                    }

                    try
                    {
                        TLGdata.field = ISOTaskFile.Root.Descendants("PFD").Where(pdf => pdf.Attribute("A").Value == TSK.Attribute("E").Value).Single().Attribute("C").Value;
                    }
                    catch
                    {
                        TLGdata.field = "";
                    }


                    if (ISOTaskFile.Root.Descendants("FRM").Count() > 0) {
                        TLGdata.farm = ISOTaskFile.Root.Descendants("FRM").Where(frm => frm.Attribute("A").Value == TSK.Attribute("D").Value).Single().Attribute("B").Value;
                    }

                    TLGdata.products = products;

                    List<Dictionary<string, string>> devicelist = new List<Dictionary<string, string>>();
                    List<string> devicerefs = TSK.Elements("DAN").Attributes("C").Select(attr => attr.Value).ToList();

                    foreach (var deviceref in devicerefs)
                    {
                        Dictionary<string, string> devicedict = new Dictionary<string, string>();
                        string devicename = ISOTaskFile.Root.Descendants("DVC").Single(dvc => dvc.Attribute("A").Value == deviceref).Attribute("B").Value;
                        string clientname = ISOTaskFile.Root.Descendants("DVC").Single(dvc => dvc.Attribute("A").Value == deviceref).Attribute("D").Value;
                        devicedict.Add("device", devicename);
                        devicedict.Add("clientname", clientname);
                        devicelist.Add(devicedict);
                    }

                    TLGdata.devices = devicelist;

                    // read header
                    string header_file = FindFile(directory, TLG.Attribute("A").Value + ".xml");

                    XDocument TLGFile;
                    try
                    {
                        TLGFile = XDocument.Load(header_file);
                    }
                    catch (System.IO.FileNotFoundException e)
                    {
                        Console.WriteLine(e.Message);
                        return TLGList;
                    }

                    if (TLGFile.Element("TIM").Attribute("A").Value == "")
                    {
                        TLGdata.datalogheader.Add(new LogElementType<System.String>("TimeStartTOFD"));
                        TLGdata.datalogheader.Add(new LogElementType<System.String>("TimeStartDATE"));
                    }
                    // Attribute B and C are not valid for TIM in TLG


                    foreach (var PTN in TLGFile.Element("TIM").Descendants("PTN"))
                    {
                        if (PTN.Attribute("A") != null && PTN.Attribute("A").Value == "")
                            TLGdata.datalogheader.Add(new LogElementType<System.Int32>("PositionNorth"));
                        if (PTN.Attribute("B") != null && PTN.Attribute("B").Value == "")
                            TLGdata.datalogheader.Add(new LogElementType<System.Int32>("PositionEast"));
                        if (PTN.Attribute("C") != null && PTN.Attribute("C").Value == "")
                            TLGdata.datalogheader.Add(new LogElementType<System.Int32>("PositionUp"));
                        if (PTN.Attribute("D") != null && PTN.Attribute("D").Value == "")
                            TLGdata.datalogheader.Add(new LogElementType<System.Byte>("PositionStatus"));
                        if (PTN.Attribute("E") != null && PTN.Attribute("E").Value == "")
                            TLGdata.datalogheader.Add(new LogElementType<System.UInt16>("PDOP"));
                        if (PTN.Attribute("F") != null && PTN.Attribute("F").Value == "")
                            TLGdata.datalogheader.Add(new LogElementType<System.UInt16>("HDOP"));
                        if (PTN.Attribute("G") != null && PTN.Attribute("G").Value == "")
                            TLGdata.datalogheader.Add(new LogElementType<System.Byte>("NumberOfSatellites"));
                        if (PTN.Attribute("H") != null && PTN.Attribute("H").Value == "")
                            TLGdata.datalogheader.Add(new LogElementType<System.String>("GpsUtcTime"));
                        if (PTN.Attribute("I") != null && PTN.Attribute("I").Value == "")
                            TLGdata.datalogheader.Add(new LogElementType<System.String>("GpsUtcDate"));
                    }


                    foreach (var DLV in TLGFile.Element("TIM").Descendants("DLV"))
                    {
                        string ProcessDataDDI = DLV.Attribute("A").Value;
                        string DeviceElementIdRef = DLV.Attribute("C").Value;

                        var DET = ISOTaskFile.Root.Descendants("DET").Where(det => det.Attribute("A").Value == DeviceElementIdRef).Single();
                        try
                        {
                            var DOR = DET.Descendants("DOR").Attributes("A").Select(atr => atr.Value).ToList();
                            var DPD = ISOTaskFile.Root.Descendants("DPD").Where(dpd => DOR.Contains(dpd.Attribute("A").Value) && dpd.Attribute("B").Value == ProcessDataDDI).Single();

                            string DVCdesignator = DET.Parent.Attribute("B").Value;  // Note! optional attribute
                            string DETdesignator = DET.Attribute("D").Value; // Note! optional attribute
                            string DPDdesignator = DPD.Attribute("E").Value; // Note! optional attribute --> Use DDI description instead
                            string DETno = DET.Attribute("A").Value;

                            LogElementType<System.Int32> logelement = new LogElementType<System.Int32>(DPDdesignator,
                                DVCdesignator, DETdesignator, DETno, ProcessDataDDI);
                            try
                            {
                                logelement.values.Add(System.Int32.Parse(DLV.Attribute("B").Value));
                            }
                            catch (System.FormatException)
                            {
                                logelement.values.Add(0);
                            }
                            TLGdata.datalogdata.Add(logelement);
                        }
                        catch (System.InvalidOperationException)
                        {
                            //Console.WriteLine("Process data description not found!");

                            LogElementType<System.Int32> logelement = new LogElementType<System.Int32>(DeviceElementIdRef,
                                    "", "", "", ProcessDataDDI);
                            try
                            {
                                logelement.values.Add(System.Int32.Parse(DLV.Attribute("B").Value));
                            }
                            catch (System.FormatException)
                            {
                                logelement.values.Add(0);
                            }
                            TLGdata.datalogdata.Add(logelement);
                        }
                    }


                    //Console.WriteLine(TLG.Attribute("A").Value);
                    /*Console.WriteLine("******* Header: **********");
                    foreach (var element in TLGdata.datalogheader)
                    {
                        Console.WriteLine(element.name);
                    }
                    Console.WriteLine("******* Data: **********");
                    foreach (var element in TLGdata.datalogdata)
                    {
                        Console.WriteLine(element.name);
                    }
                    Console.WriteLine("==============================================");*/

                    // read binary
                    // the binary can be missing
                    string binary_file;
                    try {
                        binary_file = FindFile(directory, TLG.Attribute("A").Value + ".BIN");
                    }
                    catch {
                        break;
                    }

                    BinaryReader reader = new BinaryReader(new FileStream(binary_file, FileMode.Open));
                    List<LogElement>.Enumerator header = TLGdata.datalogheader.GetEnumerator();

                    while (reader.BaseStream.Position < reader.BaseStream.Length)
                    {
                        if (!header.MoveNext())
                        {
                            header = TLGdata.datalogheader.GetEnumerator();
                            if (!header.MoveNext())
                            {
                                //Console.WriteLine("NO HEADER FOR DATA");
                                return TLGList;
                            }


                            System.Int32[] lastdata = new System.Int32[TLGdata.datalogdata.Count];
                            for (int i = 0; i < TLGdata.datalogdata.Count; i++)
                                lastdata[i] = ((LogElementType<System.Int32>)TLGdata.datalogdata.ElementAt(i)).values.Last();


                            System.Byte DLVs = reader.ReadByte();
                            for (int n = 0; n < DLVs; n++)
                            {
                                System.Byte DLVn = reader.ReadByte();
                                lastdata[DLVn] = reader.ReadInt32();
                            }

                            for (int i = 0; i < TLGdata.datalogdata.Count; i++)
                                ((LogElementType<System.Int32>)TLGdata.datalogdata.ElementAt(i)).values.Add(lastdata[i]);

                            if (reader.BaseStream.Position >= reader.BaseStream.Length)
                                break;
                        }

                        if (header.Current.name == "TimeStartDATE" || header.Current.name == "GpsUtcDate")
                        {
                            LogElementType<System.String> element = (LogElementType<System.String>)header.Current;
                            DateTime date = new DateTime(1980, 1, 1);
                            date = date.AddDays(reader.ReadUInt16());
                            element.values.Add(date.ToString("yyyy-MM-dd"));
                        }
                        else if (header.Current.name == "TimeStartTOFD" || header.Current.name == "GpsUtcTime")
                        {
                            LogElementType<System.String> element = (LogElementType<System.String>)header.Current;
                            DateTime date = new DateTime();
                            date = date.AddMilliseconds(reader.ReadUInt32());
                            element.values.Add(date.ToString("HH:mm:ss.fff", CultureInfo.InvariantCulture));
                        }
                        else if (header.Current.type == typeof(System.Byte))
                        {
                            LogElementType<System.Byte> element = (LogElementType<System.Byte>)header.Current;
                            element.values.Add(reader.ReadByte());
                        }
                        else if (header.Current.type == typeof(System.Int16))
                        {
                            LogElementType<System.Int16> element = (LogElementType<System.Int16>)header.Current;
                            element.values.Add(reader.ReadInt16());
                        }
                        else if (header.Current.type == typeof(System.Int32))
                        {
                            LogElementType<System.Int32> element = (LogElementType<System.Int32>)header.Current;
                            element.values.Add(reader.ReadInt32());
                        }
                        else if (header.Current.type == typeof(System.UInt16))
                        {
                            LogElementType<System.UInt16> element = (LogElementType<System.UInt16>)header.Current;
                            element.values.Add(reader.ReadUInt16());
                        }
                        else if (header.Current.type == typeof(System.UInt32))
                        {
                            LogElementType<System.UInt32> element = (LogElementType<System.UInt32>)header.Current;
                            element.values.Add(reader.ReadUInt32());
                        }
                        else if (header.Current.type == typeof(System.UInt64))
                        {
                            LogElementType<System.UInt64> element = (LogElementType<System.UInt64>)header.Current;
                            element.values.Add(reader.ReadUInt64());
                        }
                    }

                    foreach (var element in TLGdata.datalogdata)
                    {
                        LogElementType<System.Int32> data = (LogElementType<System.Int32>)element;
                        data.values.RemoveAt(0);
                    }

                    reader.Close();
                    TLGList.Add(TLGdata);
                }
                }


            }
            return TLGList;
        }
    }
}
