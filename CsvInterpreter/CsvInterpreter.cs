namespace CsvInterpreter
{
    public class CsvInterpreter(Stream fileDataStream)
    {
        private Dictionary<int, string> _propertyMap = new Dictionary<int, string>();

        public async Task<List<T>> ParseCsvDataToType<T>() where T : new()
        {
            var genericDataObjectList = new List<T>();

            MemoryStream ms = new MemoryStream();
            await fileDataStream.CopyToAsync(ms);
            fileDataStream.Close();
            var outputFileString = System.Text.Encoding.UTF8.GetString(ms.ToArray());
            var lineSeparator = outputFileString.Contains("\r\n") ? "\r\n" : "\n";

            int count = 0;
            foreach (var item in outputFileString.Split(lineSeparator))
            {
                if (count == 0)
                {
                    GenerateFirstLineObjectMap<T>(item);
                }
                if (count > 1 && !string.IsNullOrEmpty(item))
                {
                    genericDataObjectList.Add(MapCsvLineToGenericObject<T>(item));
                }

                count++;
            }

            return genericDataObjectList;
        }

        private T MapCsvLineToGenericObject<T>(string csvLine) where T : new()
        {
            var genericObjectData = new T();

            int count = 0;
            /*
             * For each position we are at in the csv header, we get the object property name from the dictionary of properties using the index (key).
             * We know this will match the header data because we generated the dictionary from the header data, and it should have the same index position as the dictionary item.
             * This dictionary technique saves us the expense of looping through the generic object properties with each loop of the csv data.
             */
            foreach (var item in csvLine.Split(","))
            {
                if (!_propertyMap.ContainsKey(count))
                {
                    count++;
                    continue;
                }
                var mappedProperty = _propertyMap[count];
                var propType = genericObjectData.GetType().GetProperty(mappedProperty)!.PropertyType.Name;
                var fullName = genericObjectData.GetType().GetProperty(mappedProperty)!.PropertyType.FullName;
                if ((propType == "Decimal" || (fullName!.ToLower().Contains("decimal"))) && item != string.Empty)
                {
                    genericObjectData.GetType().GetProperty(mappedProperty)!.SetValue(genericObjectData, decimal.Parse(item));
                }
                if (propType == "int" || (fullName!.ToLower().Contains("int")))
                {
                    genericObjectData.GetType().GetProperty(mappedProperty)!.SetValue(genericObjectData, int.Parse(item));
                }
                if (propType == "String")
                {
                    genericObjectData.GetType().GetProperty(mappedProperty)!.SetValue(genericObjectData, item);
                }
                if (propType == "DateTime" || (mappedProperty.ToLower().Contains("date") && DateTime.TryParse(item, out var dtParseResult)))
                {
                    var dateVal = DateTime.Parse(item);
                    genericObjectData.GetType().GetProperty(mappedProperty)!.SetValue(genericObjectData, dateVal);
                }
                count++;
            }

            return genericObjectData;
        }
        //This function assumes that the object properties have the same name as the csv header columns.
        //The function will attempt to match the csv header columns to the object properties.
        private void GenerateFirstLineObjectMap<T>(string firstLine)
        {
            var list = firstLine.Split(',');
            var propList = typeof(T).GetProperties().ToList();
            int count = 0;

            /*
             * Loop through the csv header items and attempt to match them to the object properties.
             * We add them to a dictionary with the index of the csv header item as the key and the object property name as the value.
             * This dictionary will be used later as we loop through each line of the csv file to map the csv data to the object properties.
             */
            foreach (var csvHeaderItem in list)
            {
                if (propList.Any(p => p.Name == csvHeaderItem.Replace(" ", "")))
                {
                    var propString = propList.Single(p => p.Name == csvHeaderItem.Replace(" ", "")).Name;
                    _propertyMap.Add(count, propString);
                }

                //try and guess the date property (since none will probably match)
                //this is for cases in which a date type property on the object is named differently than the date type in the csv header
                //generally, the program should expect object properties to match
                //this could probably be taken care of through inheritance on the passed in object
                if (csvHeaderItem.Replace(" ", "").ToLower().Contains("date") && propList.All(p => p.Name != csvHeaderItem.Replace(" ", "")))
                {
                    var result = propList.Any(p => p.Name.ToLower().Contains("date")) ?
                        propList.Single(p => p.Name.ToLower().Contains("date")).Name : null;
                    if (!string.IsNullOrEmpty(result))
                    {
                        _propertyMap.Add(count, result);
                    }
                }
                count++;
            }
        }
    }
}
