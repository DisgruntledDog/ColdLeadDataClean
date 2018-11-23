using CsvHelper;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ColdLeadDataClean
{
    public class CleanInteractions
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public void Clean(string filename)
        {
            log.Info("Begin Interaction File Clean");

            Dictionary<string, Interaction> cleaned = new Dictionary<string, Interaction>();
            List<Interaction> errors = new List<Interaction>();
            using (TextReader reader = File.OpenText(filename))
            {
                CsvReader csv = new CsvReader(reader);
                csv.Configuration.Delimiter = ",";
                log.Info($"Opened {filename}");

                csv.Configuration.MissingFieldFound = null;
                csv.Configuration.BadDataFound = null;
                csv.Configuration.HasHeaderRecord = true;

                csv.Read();
                csv.ReadHeader();
                var interactions = csv.GetRecords<Interaction>();
                int i = 1;
                foreach (var interaction in interactions)
                {
                    if (cleaned.ContainsKey(interaction.ID))
                    {
                        log.Warn($"The Row {interaction} already contains the ID {interaction.ID}");
                         errors.Add(interaction);
                    }
                    else
                    {
                        cleaned.Add(interaction.ID, interaction);
                    }
                }
            }
            this.WriteCleaned(cleaned);

            string errorFile = @"Output\\InteractionErrors.csv";
            if (File.Exists(errorFile)) File.Delete(errorFile);

            using (TextWriter writer = new StreamWriter(errorFile, false, System.Text.Encoding.UTF8))
            {

                var err = new CsvWriter(writer);
                err.Configuration.Delimiter = ",";
                err.Configuration.HasHeaderRecord = true;

                err.WriteRecords(errors);
                err.Flush();
            }


        }

        private void WriteCleaned(Dictionary<string, Interaction> cleaned)
        {

            log.Info("Writing Cleansed records to file to file.");
            int rowsThisFile = int.MaxValue;
            int fileNumber = 0;

            var listOfLists = new List<List<Interaction>>();
            List<Interaction> newCleandedList = null;
            foreach (KeyValuePair<string, Interaction> entry in cleaned)
            {
                if (rowsThisFile >= 5000)
                {
                    newCleandedList = new List<Interaction>();
                    listOfLists.Add(newCleandedList);
                    rowsThisFile = 0;
                }
                newCleandedList.Add(entry.Value);
                rowsThisFile++;
            }

            foreach (List<Interaction> cleanList in listOfLists)
            {
                string filename = $"output\\CleanInteractions{++fileNumber}.csv";
                if (File.Exists(filename)) File.Delete(filename);
                using (TextWriter writer = new StreamWriter(filename, false, Encoding.UTF8))
                {

                    var csv = new CsvWriter(writer);
                    // csv.Configuration.Delimiter = "\t";
                    csv.Configuration.QuoteNoFields = true;

                    csv.WriteRecords(cleanList); // where values implements IEnumerable
                }

            }
            log.Info("CleanedInteractions Writing Complete");
        }
    }
}
