using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace BulkSMS
{
    class Program
    {
        static List<String> SenderIDs = new List<String> { "Montana", "Tony"};
        const String ConstantFileID = "CONTACTS.TXT";
        static String Token { get; set; }
        static void Main(string[] args)
        {
#if ONE_DEFINED
                FileInfo oldFile = new FileInfo($"{Directory.GetCurrentDirectory()}\\contacts.csv");
                FileInfo newFile = new FileInfo($"{Directory.GetCurrentDirectory()}\\newContacts.csv");
                CopyContacts(oldFile, newFile);
#endif
            if(args.Length > 0)
            {
                Token = args[0];
            }

            while (true)
            {
                String senderId = RetreiveSenderID();
                if (String.IsNullOrEmpty(senderId)) break;

                String receivers = GetContactLists();
                if (String.IsNullOrEmpty(receivers)) break;

                String message = RetrieveMessage();
                var status = SendSMS(senderId, message, receivers, Token).Result;

                ShowSummary(senderId, receivers, message, JsonConvert.SerializeObject(status, Formatting.Indented));
            }
        }

        private static void CopyContacts(FileInfo oldFile, FileInfo newFile)
        {
            if (oldFile.Exists)
            {

                using var fileStream = oldFile.OpenText();
                using var writeStream = newFile.OpenWrite();
                var currentLine = fileStream.ReadLine();
                var stringBuilder = new List<String>();

                while (!String.IsNullOrEmpty(currentLine))
                {
                    currentLine = fileStream.ReadLine();
                    var newContact = writeStream.AppendNewContact(currentLine);

                    if (String.IsNullOrEmpty(newContact) || stringBuilder.Contains(newContact))
                    {
                        continue;
                    }

                    stringBuilder.Add(newContact);
                    if (newContact.Contains(" "))
                    {
                        var allContacts = newContact.Split(" ");
                        foreach (var contact in allContacts)
                        {
                            if (contact.Trim().Length > 10)
                                writeStream.Write(Encoding.UTF8.GetBytes($"{contact.Trim()}\n"));
                        }
                        continue;
                    }
                    if (newContact.Trim().Length > 10)
                        writeStream.Write(Encoding.UTF8.GetBytes($"{newContact.Trim()}\n"));
                }
            }
        }

        private static void ShowSummary(string senderId, string receivers, string message, string status)
        {
            Console.WriteLine(
                    $"--------------\n" +
                    $"Summary" +
                    $"\n--------------\n" +
                    $"From : {senderId}\n" +
                    $"To : {receivers}\n" +
                    $"Message : {message}\n\n" +
                    $"------------------------------------------\n" +
                    $"Result : {status}\n" +
                    $"------------------------------------------"
                  );
            Console.ReadLine();
        }
        private static String RetrieveMessage()
        {
            Console.Clear();
            Console.WriteLine("Enter Message : ");
            String message = Console.ReadLine();
            return message;
        }
        static String RetreiveSenderID()
        {
            Console.Clear();
            int senderIndex = 0;
            Console.WriteLine("Select Sender ID :");
            foreach (var id in SenderIDs)
            {
                Console.WriteLine($"{++senderIndex} : {id}");
            }


            if (!Int32.TryParse(Console.ReadLine(), out int sender) || --sender >= SenderIDs.Count || sender < 0)
            {
                return String.Empty;
            }
            return SenderIDs[sender];
        }
        static async Task<BulkResponse> SendSMS(String sender, String message, String receivers, String OrigToken)
        {
            try
            {
                using var httpClient = new HttpClient();
                if(string.IsNullOrEmpty(OrigToken))
                {
                    OrigToken = "JtGPOYyBJ6MBO2favllQJeAHFZXhRRuZWbujHKuvyX5C10DMou4aak2PxInL";
                }
                String baseUrl = "https://www.bulksmsnigeria.com/api/v1/sms/create";
                String token = $"api_token={OrigToken}";
                sender = $"from={sender}";
                receivers = $"to={receivers}";
                message = $"body={message}";

                var response = await httpClient.GetAsync($"{baseUrl}?{sender}&{token}&{receivers}&{message}&dnd=2");
                var result = JsonConvert.DeserializeObject<BulkResponse>(await response.Content.ReadAsStringAsync());
                result.Token = OrigToken;
                return result;
            }
            catch(Exception ex)
            {
                return new BulkResponse
                {
                    data = new BulkData
                    {
                        message = ex.InnerException?.Message ?? ex.Message
                    },
                    Token = OrigToken
                };
            }
        }
        static String GetContactLists(String delimiter = ",")
        {
            Console.Clear();
            List<String> contactList =  Directory.GetFiles(Directory.GetCurrentDirectory())
                                            .Where(X => X.ToUpper().Contains(ConstantFileID)).ToList();

            String selectedList = contactList.ShowContacts(ConstantFileID, Console.WriteLine);

            if (String.IsNullOrEmpty(selectedList))
            {
                return selectedList;
            }

            return selectedList.GetAllContacts(delimiter);
        }
     
    }

    public static class Contacts
    {
        public static String TrimFileName(this String fileName, Int32 index, String trimmer)
        {
            var eFile = fileName.Split("\\").Last().ToUpper();

            return $"{++index} : {eFile.Remove(eFile.IndexOf(trimmer))}";
        }

        public static String ShowContacts(this List<String> contactList, String trimmer, Action<String> Writer)
        {
            if (contactList.Count == 0) return String.Empty;

            StringBuilder display = new StringBuilder();
            display.AppendLine("Select ContactList : ");
            for (var i = 0; i < contactList.Count; i++)
            {
                display.AppendLine($"{contactList[i].TrimFileName(i, trimmer)}");
            }

            Writer(display.ToString());

            if (!Int32.TryParse(Console.ReadLine(), out int selectedContact) || --selectedContact < 0 || selectedContact > contactList.Count)
            {
                return String.Empty;
            }

            return contactList[selectedContact];
        }

        public static String GetAllContacts(this String fileName, String delimiter)
        {
            StringBuilder result = new StringBuilder();
            FileInfo file = new FileInfo(fileName);

            using var fileStream = file.OpenText();
            var currentLine = fileStream.ReadLine();
            result.Append(currentLine);
            currentLine = fileStream.ReadLine();

            while (!String.IsNullOrEmpty(currentLine))
            {
                result.Append(delimiter);
                result.Append(currentLine);
                currentLine = fileStream.ReadLine();
            }

            return result.ToString();
        }
        
        public static String AppendNewContact(this FileStream writer, String unformated)
        {
            if(String.IsNullOrEmpty(unformated))
            {
                return unformated;
            }

            var contactInfo = unformated.Split(",");
            StringBuilder name = new StringBuilder();
            StringBuilder contact = new StringBuilder();

            foreach (var info in contactInfo)
            {
                var newInfo = info.Trim().Replace(" ", String.Empty) ;

                double currentNumber;
                if (Double.TryParse(newInfo, out currentNumber))
                {
                    switch(newInfo.Length)
                    {
                        case 10:
                            contact.AppendLine($"0{newInfo}");
                            break;
                        case 13:
                            contact.AppendLine($"0{newInfo.Substring(3)}");
                            break;
                        case 14:
                            contact.AppendLine($"0{newInfo.Substring(4)}");
                            break;
                        case 11:
                            contact.AppendLine(newInfo);
                            break;
                        default:
                            break;
                    }

                    continue;
                }

                name.AppendLine(newInfo);
            }
            var formattedName = name.ToString().Replace('\n', ' ').Replace('\r', ' ').Trim();
            var formattedNumber = contact.ToString().Replace('\n', ' ').Replace('\r', ' ').Trim();

            if(formattedNumber.Length >= 11)
            {
                var formated = $"{formattedName}, {formattedNumber}\n";
                //writer.Write(Encoding.UTF8.GetBytes(formated));
                Console.WriteLine(formated);
                return formattedNumber;
            }

            return String.Empty;
        }
    }

    public class BulkData
    {
        public string status { get; set; }
        public string message { get; set; }
        public string message_id { get; set; }
        public double cost { get; set; }
    }

    public class BulkResponse
    {
        public BulkData data { get; set; }
        public int _0 { get; set; }
        public String Token { get; set; }
    }
}
