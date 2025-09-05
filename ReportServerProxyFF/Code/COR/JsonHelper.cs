
namespace _COR.Tools.JSON
{


    public class JsonHelper
    {


        public static string Serialize(object target)
        {
#if DEBUG
            return Serialize(target, true);
#else
            return Serialize(target, false);
#endif
        }


        public static string Serialize(object target, string callback)
        {
#if DEBUG
            return Serialize(target, true, callback);
#else
            return Serialize(target, false,callback);
#endif
        }


        public static void Serialize(object target, System.IO.TextWriter pOutput)
        {
#if DEBUG
            Serialize(target, true, pOutput);
#else
            Serialize(target, false, pOutput);
#endif
        }


        public static void Serialize(object target, System.IO.TextWriter pOutput, string callback)
        {
#if DEBUG
            Serialize(target, true, pOutput, callback);
#else
            Serialize(target, false, pOutput, callback);
#endif
        }


        public static string Serialize(object target, bool prettyPrint)
        {
            return Serialize(target, prettyPrint, (string)null);
        }


        // Support for JSON-P
        public static string Serialize(object target, bool prettyPrint, string callback)
        {
            string strResult = null;

            Newtonsoft.Json.JsonSerializerSettings settings = new Newtonsoft.Json.JsonSerializerSettings();
            settings.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;

            if (prettyPrint)
            {
                settings.Formatting = Newtonsoft.Json.Formatting.Indented;
            }
            else
            {
                settings.Formatting = Newtonsoft.Json.Formatting.None;
            }

            settings.DateFormatHandling = Newtonsoft.Json.DateFormatHandling.MicrosoftDateFormat;

            if (string.IsNullOrEmpty(callback))
            {
                strResult = Newtonsoft.Json.JsonConvert.SerializeObject(target, settings);
                settings = null;
                return strResult;
            }


            //  JSONP
            //  https://github.com/visionmedia/express/pull/1374
            // strResult = strCallback + " && " + strCallback + "(" + Newtonsoft.Json.JsonConvert.SerializeObject(target, settings) + "); " + System.Environment.NewLine
            // typeof bla1 != "undefined" ? alert(bla1(3)) : alert("foo undefined");
            strResult = ("typeof "
                        + (callback + (" != \'undefined\' ? "
                        + (callback + ("("
                        + (Newtonsoft.Json.JsonConvert.SerializeObject(target, settings) + (") : alert(\'Callback-Funktion \""
                        + (callback + ("\" undefiniert...\'); " + System.Environment.NewLine)))))))));

            settings = null;
            return strResult;
        }


        public static void Serialize(object target, bool prettyPrint, System.IO.TextWriter pOutput)
        {
            Serialize(target, prettyPrint, pOutput, (string)null);
        }


        public static void Serialize(object target, bool prettyPrint, System.IO.TextWriter pOutput, string callback)
        {
            Newtonsoft.Json.JsonSerializer ser = new Newtonsoft.Json.JsonSerializer();

            ser.DateFormatHandling = Newtonsoft.Json.DateFormatHandling.MicrosoftDateFormat;
            ser.Formatting = prettyPrint ? Newtonsoft.Json.Formatting.Indented : Newtonsoft.Json.Formatting.None;
            ser.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;


            if (string.IsNullOrEmpty(callback))
            {
                ser.Serialize(pOutput, target);
                return;
            }

            // typeof foobar != 'undefined' ? foobar(bla) : alert('Callback-Funktion "foobar" undefiniert...'); 
            pOutput.Write("typeof ");
            pOutput.Write(callback);
            pOutput.Write(" != 'undefined' ? ");
            pOutput.Write(callback);
            pOutput.Write("(");
            ser.Serialize(pOutput, target);
            pOutput.Write(")");
            pOutput.Write(" : alert('Callback-Funktion \"");
            pOutput.Write(callback);
            pOutput.Write("\" undefiniert...');");
        }


        public static T DeserializeJArray<T>(object obj)
        {
            return ((Newtonsoft.Json.Linq.JArray)obj).ToObject<T>();
        }


        public static T Deserialize<T>(System.IO.Stream stream)
        {
            T objReturnValue = default(T);
            Newtonsoft.Json.JsonSerializer serializer = new Newtonsoft.Json.JsonSerializer();

            using (System.IO.TextReader sr = new System.IO.StreamReader(stream, System.Text.Encoding.UTF8))
            {
                using (Newtonsoft.Json.JsonReader jsonTextReader = new Newtonsoft.Json.JsonTextReader(sr))
                {
                    objReturnValue = serializer.Deserialize<T>(jsonTextReader);
                }
            }

            return objReturnValue;
        }


        public static T Deserialize<T>(string json)
        {

            return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(json);
            //REM: 4.0
            //T obj = System.Activator.CreateInstance<T>();
            //System.Web.Script.Serialization.JavaScriptSerializer JSONserializer = new System.Web.Script.Serialization.JavaScriptSerializer();
            //obj = JSONserializer.Deserialize<T>(json);
            //return obj;
        }


    }


}
