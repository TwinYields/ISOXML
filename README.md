
# .NET standard library for reading ISO 11783 Task file timelog data

Based on https://github.com/TwinYields/ISO_GML_Converter and used in https://github.com/TwinYields/farmingpy .

**Usage:**

```C#
using ISOXML;

List<TimeLogData> data = TimeLogReader.ReadTaskFile("TASKPATH/TASKDATA.XML");
```