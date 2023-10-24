using ConvertXMLtoJSON;
using Newtonsoft.Json;
using System.Linq;

Dictionary<string, Substation> Substations = new Dictionary<string, Substation>();
Dictionary<string, VoltageLevelsWithIdSubstation> VoltageLevels = new Dictionary<string, VoltageLevelsWithIdSubstation>();
List<SynchronousMachineWithIdVoltageLevel> SynchronousMachines = new List<SynchronousMachineWithIdVoltageLevel>();


StreamReader sr = new StreamReader("Example.xml");

string GetPropertyName(string line) 
{
    return line.Substring(line.IndexOf('>') + 1, line.LastIndexOf('<') - line.IndexOf('>') - 1);
}

string GetPropertyId(string line)
{
    return line.Substring(line.IndexOf('\"') + 1, line.LastIndexOf('\"') - line.IndexOf('\"') - 1);
}

while (!sr.EndOfStream) 
{
    string line = sr.ReadLine();

    //Значит имеет дело с подстанцией, которую надо добавить в словарь Substations
    if (line.Contains("<cim:Substation rdf:about")) 
    {
        string idSubstation = GetPropertyId(line);

        Substation substation = new Substation();

        //Ищем имя подстанции
        while (!line.Contains("<cim:IdentifiedObject.name>"))
            line = sr.ReadLine();

        substation.Name = GetPropertyName(line);

        Substations.Add(idSubstation, substation);
    }
    else 
    {
        //Имеем дело с распределительным устройством
        if (line.Contains("<cim:VoltageLevel rdf:about")) 
        {
            string idVoltageLevel = GetPropertyId(line);

            VoltageLevel voltageLevel = new VoltageLevel();

            string idSubstation = "";

            //Ищем имя распределительного устройства или id подстанции
            while (!line.Contains("<cim:IdentifiedObject.name>") && !line.Contains("<cim:VoltageLevel.Substation"))
                line = sr.ReadLine();
            if (line.Contains("<cim:IdentifiedObject.name>"))
                voltageLevel.Name = GetPropertyName(line);
            else
                idSubstation = GetPropertyId(line);

            line = sr.ReadLine();

            while (!line.Contains("<cim:IdentifiedObject.name>") && !line.Contains("<cim:VoltageLevel.Substation"))
                line = sr.ReadLine();
            if (line.Contains("<cim:IdentifiedObject.name>"))
                voltageLevel.Name = GetPropertyName(line);
            else
                idSubstation = GetPropertyId(line);

            VoltageLevelsWithIdSubstation vl = new VoltageLevelsWithIdSubstation();
            vl.IdSubstation = idSubstation;
            vl.voltageLevel = voltageLevel;

            VoltageLevels.Add(idVoltageLevel, vl);
        }
        else 
        {
            //Имеем дело с генератором
            if (line.Contains("cim:SynchronousMachine rdf:about"))
            {
                string idVoltageLevel = "";
                string MachineName = "";

                //Ищем имя генератора или id подстанции
                while (!line.Contains("<cim:IdentifiedObject.name>") && !line.Contains("<cim:Equipment.EquipmentContainer rdf:resource"))
                    line = sr.ReadLine();
                if (line.Contains("<cim:IdentifiedObject.name>"))
                    MachineName = GetPropertyName(line);
                else
                    idVoltageLevel = GetPropertyId(line);

                line = sr.ReadLine();

                while (!line.Contains("<cim:IdentifiedObject.name>") && !line.Contains("<cim:Equipment.EquipmentContainer rdf:resource"))
                    line = sr.ReadLine();
                if (line.Contains("<cim:IdentifiedObject.name>"))
                    MachineName = GetPropertyName(line);
                else
                    idVoltageLevel = GetPropertyId(line);

                SynchronousMachineWithIdVoltageLevel synchronousMachineWithIdVoltageLevel = new SynchronousMachineWithIdVoltageLevel();
                synchronousMachineWithIdVoltageLevel.SynchronousMachineName = MachineName;
                synchronousMachineWithIdVoltageLevel.IdVoltageLevel = idVoltageLevel;

                SynchronousMachines.Add(synchronousMachineWithIdVoltageLevel);
            }
        }
    }
}

sr.Close();

//Добавляются на этом этапе, а не в основном ввиду того, что подстанции и генераторы могут быть объявлены раньше объектов, в которые вложены
foreach (var vl in VoltageLevels) 
{
    Substations[vl.Value.IdSubstation].VoltageLevel = vl.Value.voltageLevel;
}

foreach (var sm in SynchronousMachines) 
{
    foreach (var substation in Substations)
    {
        if (substation.Value.VoltageLevel != null)
            if (substation.Value.VoltageLevel.Name == VoltageLevels[sm.IdVoltageLevel].voltageLevel.Name)
                substation.Value.VoltageLevel.Generators.Add(sm.SynchronousMachineName);
    }
}

var json = JsonConvert.SerializeObject(Substations.Values);
File.WriteAllText("Example.json", json);
