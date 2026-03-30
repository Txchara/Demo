using static Program;

public class JsonConvert<T>
{
    /// <summary>
    /// Json转换
    /// </summary>
    /// <param name="OldData">需要转换的数据</param>
    /// <returns></returns>
    internal string ToJsonObject(MyList<T> OldData)
    {
        try
        {
            string json = "{";

            for (int i = 0; i < OldData.Count; i++)
            {
                json += "\"" + i + "\":" + FormatValue(OldData[i]);

                if (i != OldData.Count - 1)
                {
                    json += ",";
                }
            }

            json += "}";

            return json;
        }
        catch (Exception)
        {

            throw;
        }
    }

    private string FormatValue(object item)
    {
        if (item == null)
            return "null";

        if (item is string)
            return "\"" + item.ToString()?.Replace("\"", "\\\"") + "\"";

        if (item is bool)
            return item.ToString() ?? "".ToLower();

        return item.ToString() ?? "";
    }
}