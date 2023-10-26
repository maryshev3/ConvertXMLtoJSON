using ConvertXMLtoJSON;
using Newtonsoft.Json;
using System.Linq;

Dictionary<string, Substation> Substations = new Dictionary<string, Substation>();
Dictionary<string, VoltageLevelsWithIdSubstation> VoltageLevels = new Dictionary<string, VoltageLevelsWithIdSubstation>();
List<SynchronousMachineWithIdVoltageLevel> SynchronousMachines = new List<SynchronousMachineWithIdVoltageLevel>();


StreamReader sr = new StreamReader("../../../Example.xml");

string GetPropertyName(string line) 
{
    return line.Substring(line.IndexOf('>') + 1, line.LastIndexOf('<') - line.IndexOf('>') - 1);
}

string GetPropertyId(string line)
{
    return line.Substring(line.IndexOf('\"') + 1, line.LastIndexOf('\"') - line.IndexOf('\"') - 1);
}

string GetAttribute(StreamReader sr) 
{
    bool isEndedAttribute = false;

    string Attribute = "";
    char StartChar = (char)sr.Read();

    while (StartChar != '<')
        StartChar = (char)sr.Read();

    Attribute += StartChar;

    while (!isEndedAttribute)
    {
        StartChar = (char)sr.Read();

        while (StartChar != '>')
        {
            Attribute += StartChar;

            StartChar = (char)sr.Read();

            if (StartChar == '/')
                isEndedAttribute = true;
        }

        if (!Attribute.Contains("IdentifiedObject.name"))
            isEndedAttribute = true;

        Attribute += StartChar;
    }

    return Attribute;
}

AttributesType GetTypeAttribute(string Attribute) 
{
    if (Attribute.Contains("Substation"))
        return AttributesType.Substation;
    if (Attribute.Contains("VoltageLevel"))
        return AttributesType.VoltageLevels;
    if (Attribute.Contains("SynchronousMachine"))
        return AttributesType.Synchronisation;

    return AttributesType.Any;
}

while (!sr.EndOfStream) 
{
    string line = GetAttribute(sr);

    if (line.Contains("rdf:about"))
        switch (GetTypeAttribute(line)) 
        {
            case AttributesType.Substation: 
                {
                    string idSubstation = GetPropertyId(line);

                    Substation substation = new Substation();

                    //Ищем имя подстанции
                    while (!line.Contains("IdentifiedObject.name"))
                        line = GetAttribute(sr);

                    substation.Name = GetPropertyName(line);

                    bool isHave = false;
                    foreach (var st in Substations)
                        if (st.Value.Name == substation.Name)
                        {
                            isHave = true;
                            break;
                        }

                    if (!isHave)
                        Substations.Add(idSubstation, substation);
                } break;

            case AttributesType.VoltageLevels:
                {
                    string idVoltageLevel = GetPropertyId(line);

                    VoltageLevel voltageLevel = new VoltageLevel();

                    string idSubstation = "";

                    //Ищем имя распределительного устройства или id подстанции
                    for (int i = 0; i < 2; i++)
                    {
                        while (!line.Contains("IdentifiedObject.name") && !line.Contains("VoltageLevel.Substation"))
                            line = GetAttribute(sr);
                        if (line.Contains("IdentifiedObject.name"))
                            voltageLevel.Name = GetPropertyName(line);
                        else
                            idSubstation = GetPropertyId(line);

                         if (i == 0)
                            line = GetAttribute(sr);
                    }

                    VoltageLevelsWithIdSubstation vl = new VoltageLevelsWithIdSubstation();
                    vl.IdSubstation = idSubstation;
                    vl.voltageLevel = voltageLevel;

                    VoltageLevels.Add(idVoltageLevel, vl);
                } break;

            case AttributesType.Synchronisation:
                {
                    string idVoltageLevel = "";
                    string MachineName = "";

                    //Ищем имя генератора или id подстанции
                    for (int i = 0; i < 2; i++)
                    {
                        while (!line.Contains("IdentifiedObject.name>") && !line.Contains("Equipment.EquipmentContainer"))
                            line = GetAttribute(sr);
                        if (line.Contains("IdentifiedObject.name>"))
                            MachineName = GetPropertyName(line);
                        else
                            idVoltageLevel = GetPropertyId(line);

                        if (i == 0)
                            line = GetAttribute(sr);
                    }

                    SynchronousMachineWithIdVoltageLevel synchronousMachineWithIdVoltageLevel = new SynchronousMachineWithIdVoltageLevel();
                    synchronousMachineWithIdVoltageLevel.SynchronousMachineName = MachineName;
                    synchronousMachineWithIdVoltageLevel.IdVoltageLevel = idVoltageLevel;

                    SynchronousMachines.Add(synchronousMachineWithIdVoltageLevel);
                } break;

            case AttributesType.Any:
                { 

                } break;
        }
}

sr.Close();

//Добавляются на этом этапе, а не в основном ввиду того, что подстанции и генераторы могут быть объявлены раньше объектов, в которые вложены
foreach (var vl in VoltageLevels) 
{
    if (Substations.ContainsKey(vl.Value.IdSubstation))
        Substations[vl.Value.IdSubstation].VoltageLevel = vl.Value.voltageLevel;
}

foreach (var sm in SynchronousMachines) 
{
    foreach (var substation in Substations)
    {
        try
        {
            if (substation.Value.VoltageLevel != null)
                if (substation.Value.VoltageLevel.Name == VoltageLevels[sm.IdVoltageLevel].voltageLevel.Name)
                    if (!substation.Value.VoltageLevel.Generators.Contains(sm.SynchronousMachineName))
                        substation.Value.VoltageLevel.Generators.Add(sm.SynchronousMachineName);
        }
        catch { }
    }
}

var json = JsonConvert.SerializeObject(Substations.Values);
File.WriteAllText("Example.json", json);
