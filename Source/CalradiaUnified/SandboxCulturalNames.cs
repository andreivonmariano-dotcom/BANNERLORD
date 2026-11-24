using System;
using System.Collections.Generic;

namespace CalradiaUnified
{
    /// <summary>
    /// Deterministic culturally matched name suggestions for new Heroes.
    /// Every suggested family name remains editable in Advanced settings.
    /// </summary>
    public static class SandboxCulturalNames
    {
        private static string[] Pool(string culture,bool female)
        {
            switch((culture??"").ToLowerInvariant())
            {
                case "vlandia":return female ?
                    new[]{"Aveline","Ysabel","Elodie","Margot","Rosamund",
                        "Beatrice","Adeline","Cecile","Isolde","Mathilde"} :
                    new[]{"Aldric","Gervais","Mathieu","Renaud","Bertran",
                        "Osric","Hugues","Roland","Thibaut","Gaston"};
                case "sturgia":return female ?
                    new[]{"Vesna","Milena","Yelena","Darya","Zorya",
                        "Rada","Irina","Nadezhda","Vasilisa","Miroslava"} :
                    new[]{"Mstislav","Oleg","Dobrynya","Vladimir","Rurik",
                        "Bogdan","Yaroslav","Vsevolod","Miroslav","Radomir"};
                case "battania":return female ?
                    new[]{"Nessa","Ailis","Maeve","Eira","Brigid",
                        "Rhiannon","Fiona","Sorcha","Arianwen","Enid"} :
                    new[]{"Caelan","Branoc","Eogan","Finian","Aedan",
                        "Taliesin","Cadan","Bryn","Torin","Ciaran"};
                case "empire":return female ?
                    new[]{"Livia","Flavia","Aurelia","Julia","Helena",
                        "Sabina","Octavia","Antonia","Claudia","Lucilla"} :
                    new[]{"Cassian","Valerius","Lucian","Gaius","Marcellus",
                        "Decimus","Tiberius","Aelius","Quintus","Severus"};
                case "aserai":return female ?
                    new[]{"Zahra","Layla","Amira","Nadira","Samira",
                        "Safiya","Yasmin","Farida","Khadija","Nura"} :
                    new[]{"Faris","Yahya","Rashid","Malik","Samir",
                        "Nadir","Tariq","Hassan","Omar","Zahir"};
                case "khuzait":return female ?
                    new[]{"Altani","Saran","Altynai","Orda","Suyun",
                        "Qulan","Borte","Sorghag","Khulan","Sarnai"} :
                    new[]{"Bortu","Temir","Arban","Khasar","Altan",
                        "Batu","Jebe","Subutai","Arghun","Kadan"};
                // Suggestions only if an actual native Hero uses this ID.
                case "nord":return female ?
                    new[]{"Astrid","Sigrid","Ingrid","Ragnhild","Thyra",
                        "Gudrun","Freydis","Helga","Signe","Yrsa"} :
                    new[]{"Harald","Eirik","Bjorn","Leif","Ivar",
                        "Hakon","Ragnar","Sigurd","Ulf","Halfdan"};
                default:return female ?
                    new[]{"Elira","Mira","Leora","Nadia","Rowena",
                        "Isara","Seren","Talia","Yara","Lena"} :
                    new[]{"Aldren","Rowan","Taran","Daric","Daren",
                        "Beric","Oren","Tomas","Galen","Edrin"};
            }
        }

        public static string Suggest(string culture,bool female,int seed,
            IEnumerable<string> used)
        {
            var names=Pool(culture,female);
            var excluded=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if(used!=null)
                foreach(var previous in used)
                    if(!String.IsNullOrWhiteSpace(previous))
                        excluded.Add(previous);
            int offset=Math.Abs(seed%names.Length);
            for(int i=0;i<names.Length;i++)
            {
                string next=names[(offset+i)%names.Length];
                if(!excluded.Contains(next))return next;
            }
            return names[offset]+" the Younger";
        }
    }
}
