using CsvHelper;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ColdLeadDataClean
{
    public class CleanContacts
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public void Clean(string filename)
        {
 
            foreach (string fname in Directory.GetFiles("Output"))
            {
                if (!fname.EndsWith(".log"))
                    File.Delete(fname);
            }

            log.Info("Begin File Clean");

            Dictionary<string, Contact> cleaned = new Dictionary<string, Contact>();
            List<Contact> errors = new List<Contact>();
            using (TextReader reader = File.OpenText(filename))
            {
                CsvReader csv = new CsvReader(reader);
                csv.Configuration.Delimiter = "\t";
                log.Info($"Opened {filename}");

                csv.Configuration.MissingFieldFound = null;
                csv.Configuration.BadDataFound = null;
                csv.Configuration.HasHeaderRecord = true;

                csv.Read();
                csv.ReadHeader();
                var contacts = csv.GetRecords<Contact>();
                int i = 1;
                foreach (var c in contacts)
                {
                    string s = c.STREET;
                    c.STREET = s.Replace(",", " ").Replace("\"", "").Replace("\n", " ");
                    this.FormatDateOfBirth(c);
                    this.FormatPhone(c);
                    if (cleaned.ContainsKey(c.ID))
                    {

                       errors.Add(c);
                       log.Warn($"The Row {errors.Count} in the error file already contains the ID {c.ID}");
                    }
                    else
                    {
                        if (this.IsValidEmail(c.ID))
                        {
                            cleaned.Add(c.ID, c);
                        }
                        else
                        {
                            errors.Add(c);
                            log.Warn($"The Row {errors.Count} in the error file has an invalid Email Address {c.ID}");
                        }
                    }
                }
            }
            this.WriteCleaned(cleaned);

            string errorFile = @"Output\\yMarketingUploadErrors.csv";
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

        public void FormatDateOfBirth(Contact c)
        {
            string[] dateBits = c.DATE_OF_BIRTH.Split('/');
            if (dateBits.Length == 3)
            {
                c.DATE_OF_BIRTH = $"{int.Parse(dateBits[2]):0000}{int.Parse(dateBits[1]):00}{int.Parse(dateBits[0]):00}";
            }
            else
            {
                c.DATE_OF_BIRTH = "";
            }
        }

        private void WriteCleaned(Dictionary<string, Contact> cleaned)
        {

            log.Info("Writing Cleansed records to file to file.");
            int rowsThisFile = int.MaxValue;
            int fileNumber = 0;

            var listOfLists = new List<List<Contact>>();
            List<Contact> newCleandedList = null;
            foreach (KeyValuePair<string, Contact> entry in cleaned)
            {
                if (rowsThisFile >= 5000)
                {
                    newCleandedList = new List<Contact>();
                    listOfLists.Add(newCleandedList);
                    rowsThisFile = 0;
                }
                newCleandedList.Add(entry.Value);
                rowsThisFile++;
            }

            foreach (List<Contact> cleanList in listOfLists)
            {
                string filename = $"output\\CleanContact{++fileNumber}.csv";
                if (File.Exists(filename)) File.Delete(filename);
                using (TextWriter writer = new StreamWriter(filename, false, Encoding.UTF8))
                {

                    var csv = new CsvWriter(writer);
                    // csv.Configuration.Delimiter = "\t";
                    csv.Configuration.QuoteNoFields = true;

                    csv.WriteRecords(cleanList); // where values implements IEnumerable
                }

            }
            log.Info("CleanedContact Writing Complete");
        }

        private void FormatPhone(Contact c)
        {

            string cleanedPhone = c.TELNR_MOBILE.ToString();
            if (cleanedPhone.Length > 0)
            {
                cleanedPhone = cleanedPhone.Replace("+", "");
                cleanedPhone = "+" + cleanedPhone;
            }
        }

        bool validEmailAddress = false;

        public bool IsValidEmail(string strIn)
        {
            validEmailAddress = true;
            if (String.IsNullOrEmpty(strIn))
                return false;

            // Use IdnMapping class to convert Unicode domain names.
            try
            {
                strIn = Regex.Replace(strIn, @"(@)(.+)$", this.DomainMapper,
                                      RegexOptions.None, TimeSpan.FromMilliseconds(200));
            }
            catch (RegexMatchTimeoutException)
            {
                return false;
            }
            if (validEmailAddress == false)
            {
                return false;
            }
            // Return true if strIn is in valid email format.
            try
            {
                validEmailAddress = Regex.IsMatch(strIn,
                      @"^(?("")("".+?(?<!\\)""@)|(([0-9a-z]((\.(?!\.))|[-!#\$%&'\*\+/=\?\^`\{\}\|~\w])*)(?<=[0-9a-z])@))" +
                      @"(?(\[)(\[(\d{1,3}\.){3}\d{1,3}\])|(([0-9a-z][-0-9a-z]*[0-9a-z]*\.)+[a-z0-9][\-a-z0-9]{0,22}[a-z0-9]))$",
                      RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(250));
            }
            catch (RegexMatchTimeoutException)
            {
                return false;
            }
            if (strIn.Contains(".com.0"))
            {
                validEmailAddress = false;
            }
            if (strIn.Contains("invalid."))
            {
                validEmailAddress = false;
            }

            if (strIn.Contains(".00"))
            {
                validEmailAddress = false;
            }
            return validEmailAddress;
        }

        private string DomainMapper(Match match)
        {
            // IdnMapping class with default property values.
            IdnMapping idn = new IdnMapping();

            string domainName = match.Groups[2].Value;
            try
            {
                domainName = idn.GetAscii(domainName);
            }
            catch (ArgumentException)
            {
                validEmailAddress = false;
            }
            return match.Groups[1].Value + domainName;
        }
    }
}
