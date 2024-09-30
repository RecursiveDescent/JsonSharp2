using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Reflection;
using System.Reflection.Emit;
using JsonSharp;

namespace JsonSharp {

      public static class extensions {

        public static void Test(this string s) {
          Console.WriteLine(s);
        }

        public static Tree ToJsonTree(this string s) {
          return Json.GetTree(s);
        }

        public static List<Node> ToJsonList(this string s) {
          return Json.GetList(s);
        }
      }

      public class JObject {

      public string Name;

      public string VType;

      private object _Value;

      public bool IsObj;

      public object Value { get {
        return _Value;
      }
      set {
        _Value = value;
      }
     }

     public JObject(string N, object V, string type = "string", bool o = false) {
       Name = N;
       Value = V;
       VType = type;
       IsObj = o;
     }
  }

  public sealed class JArray {

    private List<string> values = new List<string>();

    public string this[int ind] {
      get {
        return values[ind];
      }
    }

    public IEnumerator<string> GetEnumerator() {
      return values.GetEnumerator();
    }

    public void Add(string val) {
      values.Add(val);
    }

    public string[] ToArray() {
      return values.ToArray();
    }

    public static JArray FromArray (IEnumerable<string> ary) {
      JArray a = new JArray();

      foreach (string v in ary) {
        a.Add(v);
      }

      return a;
    }

  }

  public class Tree {

    List<Node> nodes = new List<Node>();

    public void Insert(Node n) {
      nodes.Add(n);
    }

    public Node FirstNode { get { return nodes[0]; } }

    public Node Get(string name) {
      foreach (Node n in nodes) {
    
      // Check if the child node has what we are looking for
      foreach (Node n2 in n.nodes) {
        if (n.Name == name) {
          return n2;
        }
      }

        if (n.Name == name) {
          return n;
        }
      }

      return null;
    }

  }

  public class Node { // ToDo: Add a Get function to get child nodes such as nested json

    public List<Node> nodes = new List<Node>();

    public JArray AryVal;

    public string Name;
    public string Value;
    public bool IsObj;
    public bool IsAry;
    public bool IsBool;

    public Node(Tree tr, string name, string value, bool o = false) {
      Name = name;
      Value = value;
      IsObj = o;
      tr.Insert(this);
    }

    public Node(Tree tr, string name, JArray value, bool o = false) {
      Name = name;
      AryVal = value;
      IsObj = o;
      IsAry = true;
      tr.Insert(this);
    }

    public Node(List<Node> tr, string name, JArray value, bool o = false) {
      Name = name;
      AryVal = value;
      IsAry = true;
      IsObj = o;
      tr.Add(this);
    }

    public Node(string name, JArray value, bool o = false) {
      Name = name;
      AryVal = value;
      IsAry = true;
      IsObj = o;
    }

    public Node(List<Node> tr, string name, string value, bool o = false) {
      Name = name;
      Value = value;
      IsObj = o;
      tr.Add(this);
    }

    public Node(string name, string value, bool o = false) {
      Name = name;
      Value = value;
      IsObj = o;
    }
  }

  public class Json {

    private static string RegM(string str, string patn) {
      Regex reg = new Regex(patn);

      return reg.Match(str).Groups[0].Value.Replace("'", "");
    }

    private static string RegM(string str, string patn, int grp) {
      Regex reg = new Regex(patn);

      return reg.Match(str).Groups[grp].Value.Replace("'", "");
    }

      // 
      private static List<JObject> Scan(string js) {

        if (! js.StartsWith("{") && ! js.StartsWith("[")) {
          throw new System.Exception("Malformed Input");
        }

        List<JObject> result = new List<JObject>();

        string valcache = "";

        bool entry = false;

        bool qoute = false;

        bool sep = false;

        string leftsep = "";

        string objholder = "";

        string objstring = "";

        bool ignorenextqoute = false;

        int nestcount = 0;

        string id = "";

        bool array = false;

        bool closewithary = false;

        string inary = "";

        bool skip = false;

        int objnest = 0;

        foreach (char c in js) {
          if (nestcount < 0) nestcount = 0;

          // Skip spaces, newlines, and anything escaped
          if (c == ' ' || c == '\n' || skip || c == '\t' || c == '\r') {
            if (qoute)
              valcache += c;
            if (ignorenextqoute)
              objstring += c;
            skip = false;
            continue;
          }

          // Escape the next character
          if (c == '\\') {
            skip = ! skip;
            continue;
          }

          if (c == '[' && nestcount == 0 && ! ignorenextqoute) {
            if (! entry) {
              entry = true;
              closewithary = true;
            }

            array = true;
            inary = "";
            nestcount++;
            continue;
          }

          if (c == ']') {
            nestcount--;
            array = false;

            JArray ary = new JArray();
            
            string gear = "";

            int nc = 0;

            foreach (char ch in inary) {
              if (ch == '{') {
                nc++;
              }

              if (ch == '}') {
                nc--;
              }

              if (ch == ',' && nc == 0) {
                ary.Add(gear.Trim('"').Trim('\''));
                gear = "";
                continue;
              }

               gear += ch;
            }

            // Leftovers
            ary.Add(gear.Trim('"').Trim('\''));
			
            result.Add(new JObject(leftsep, ary, "array"));

            if (closewithary && nestcount == 0) {
              entry = false;
            }

          }

          if (array) {
            inary += c;
            continue;
          }


          // There is a start bracket
          if (c == '{') {
            
            
            if (entry) {

              if (nestcount >= 1)
                  objnest++;

              ignorenextqoute = true;
              objstring += '{';
              objholder = leftsep;
              nestcount++;
              continue;
            }
            
            entry = true;
            continue;
          }

          if (c == '}') {

            if (nestcount > 0)
              nestcount--;

            if (ignorenextqoute && objnest == 0) {
              ignorenextqoute = false;
              objstring += '}';
              result.Add(new JObject(objholder, objstring, "string", true));
              continue;
            }

            if (ignorenextqoute && objnest > 0) {
              objnest--;
              objstring += '}';
              continue;
            }

            if (id == "null") {
              result.Add(new JObject(leftsep, id));
            }

            if (id == "true") {
              result.Add(new JObject(leftsep, id));
            }
            else if (id == "false") {
              result.Add(new JObject(leftsep, id));
            }

            string m = RegM(id, "[0-9]+");

            if (! String.IsNullOrEmpty(m)) {
              result.Add(new JObject(leftsep, id));
            }
            
            if (nestcount != 0) continue;

            entry = false;

            continue;
          }

          if (ignorenextqoute) {
            objstring += c;
            continue;
          }

          if (c == ',' && ! qoute) {

            if (id == "null") {
              result.Add(new JObject(leftsep, id));
            }

            if (id == "true") {
              result.Add(new JObject(leftsep, id));
            }
            else if (id == "false") {
              result.Add(new JObject(leftsep, id));
            }

            string m = RegM(id, "[0-9]+");

            if (! String.IsNullOrEmpty(m)) {
              result.Add(new JObject(leftsep, id));
            }

            sep = false;
            qoute = false;
            leftsep = "";
            valcache = "";
            objholder = "";
            objstring = "";
            id = "";
            continue;
          }

          if (c == ':' && ! qoute) {
            sep = true;
            continue;
          }

          if (c == '"' || c == '\'') {
            qoute = ! qoute;

            if (! qoute) {
              if (! sep) {
                leftsep = valcache;
                valcache = "";
                continue;
              }

              result.Add(new JObject(leftsep, valcache));

              valcache = "";
              sep = false;
            }

            continue;
          }

          if (qoute)
            valcache += c;
          if (! qoute )
            id += c;

        }

        if (nestcount - 1 > 0) {
          Console.WriteLine(nestcount);
          throw new Exception("Unclosed {");
        }

        if (qoute) {
          throw new Exception("Unclosed \"");
        }
        
        return result;

      }

      // Make a dictionary of json
      public static List<Node> GetList(string js) {
        List<Node> tr = new List<Node>();

        List<JObject> objs = Scan(js);

        foreach (JObject obj in objs) {

          // Send it off as an object
          if (obj.IsObj) {
            new Node(tr, obj.Name, obj.Value.ToString(), true);
            continue;
          }

          if (obj.VType == "array") {
            new Node(tr, obj.Name, (JArray) obj.Value, false);
            continue;
          }

          new Node(tr, obj.Name, obj.Value.ToString());
        }

        return tr;
      }

      // Make a tree of json
      public static Tree GetTree(string js) {
        Tree tr = new Tree();

        List<JObject> objs = Scan(js);

        foreach (JObject obj in objs) {

          // Send it off as an object
          if (obj.IsObj) {
            Console.Write(obj.Name + "|");
            new Node(tr, obj.Name, obj.Value.ToString(), true);
            continue;
          }

          if (obj.VType == "array") {
            new Node(tr, obj.Name, (JArray) obj.Value);
            continue;
          }

          new Node(tr, obj.Name, obj.Value.ToString());
        }

        return tr;
      }

      internal static bool IsPrim(string val) {
        if (val == "true" || val == "false" || val == "null" || RegM(val, "[0-9]+") != "") {
          return true;
        }

        return false;
      }

      public static string ToString (List<Node> ary) {

        string json = "{ ";

        int elements = 0;

        foreach (Node n in ary) {
          
          if (elements > 0) {
            json += ", ";
          }

          if (n.IsObj) {
            json += $"\"{n.Name}\": {n.Value} ";
            continue;
          }

          if (n.IsAry) {
            json += $"\"{n.Name}\": [ ";
            int aryc = 0;
            foreach (string s in n.AryVal) {
              if (aryc > 0) {
                json += ", " + s;
                aryc++;
                continue;
              }

              json += s;
              aryc++;
            }

            json += " ]";
            continue;
          }

          if (IsPrim(n.Value)) {
            json += ('"' + n.Name + '"') + (": " + n.Value);
            elements++;
            continue;
          }

          elements++;
          json += ('"' + n.Name + '"') + (": " + '"' + n.Value + '"');
        }

        return json + " }";
      }

    }

}

namespace JsonSharp2 {

  public class Json2 {

    internal static ModuleBuilder CreateModule(string name) {
      AssemblyName aName = new AssemblyName();
      aName.Name = name;

      AssemblyBuilder asm = AppDomain.CurrentDomain.DefineDynamicAssembly(aName, AssemblyBuilderAccess.Run);

      return asm.DefineDynamicModule("JsonObjects");
    }

    internal static string RegM(string str, string patn) {
      Regex reg = new Regex(patn);

      return reg.Match(str).Groups[0].Value.Replace("'", "");
    }

    internal static string RegM(string str, string patn, int grp) {
      Regex reg = new Regex(patn);

      return reg.Match(str).Groups[grp].Value.Replace("'", "");
    }

    public static object ConvertJson(string js, out Type typ) {
      if (! String.IsNullOrEmpty(RegM(js, "^[0-9]+$")) || ! String.IsNullOrEmpty(RegM(js, "^[0-9]+\\.[0-9]+$"))) {

        if (js.Contains(".")) {
          typ = typeof(double);
          return double.Parse(js);
        }

        typ = typeof(int);
        return int.Parse(js);
      }

      typ = typeof(string);
      return js;
    }

    public static object ConvertJson(string js) {
      if (! String.IsNullOrEmpty(RegM(js, "^[0-9]+$")) || ! String.IsNullOrEmpty(RegM(js, "^[0-9]+\\.[0-9]+$"))) {

        if (js.Contains(".")) {
          return double.Parse(js);
        }

        return int.Parse(js);
      }

      return js;
    }

    internal static TypeBuilder StartType(string ClassName) {
      TypeBuilder builder = CreateModule("JsonAsm").DefineType(ClassName, TypeAttributes.Public);

      //object obj = Activator.CreateInstance(t, null, null);

      //FieldInfo info = t.GetField("str", 
      //      BindingFlags.Public | BindingFlags.Instance);

      //info.SetValue(obj, "Hello!");

      return builder;
    }

    internal static Type ParseObj(TypeBuilder objtb, List<Node> nl) {
        foreach (Node n in nl) {
          
          if (n.IsObj) {
            TypeBuilder nestedtb = StartType("OBJTYPE_" + n.Name);

            Type t = ParseObj(nestedtb, Json.GetList(n.Value));

            objtb.DefineField(n.Name, t, FieldAttributes.Public);
            continue;
          }

          if (n.IsAry) {
            objtb.DefineField(n.Name, typeof(string[]), FieldAttributes.Public);
            continue;
          }
          
          objtb.DefineField(n.Name, typeof(string), FieldAttributes.Public);
        }

        Type objt = objtb.CreateType();

        return objt;
    }

    internal static object CreateObj(Type objt, List<Node> nl) {
        object objinst = Activator.CreateInstance(objt, null, null);

        foreach (Node n2 in nl) {

          FieldInfo ofi = objt.GetField(n2.Name, BindingFlags.Public | BindingFlags.Instance);

          if (n2.IsObj) {
            object obj = Activator.CreateInstance(ofi.FieldType, null, null);

            obj = CreateObj(ofi.FieldType, n2.Value.ToJsonList());

            ofi.SetValue(objinst, obj);
            continue;
          }

          ofi.SetValue(objinst, n2.Value);
        }


        return objinst;
    }

    public static object Parse(string js) {
      TypeBuilder tb = StartType("JsonRoot"); // Make the root Type

      bool First = true;

      // Define Types
      foreach (Node n in js.ToJsonList()) {

        if (First && n.IsAry && String.IsNullOrEmpty(n.Name)) {
          // Define the root if the root object is an array
          tb.DefineField("__ArrayRoot", typeof(string[]), FieldAttributes.Public);

          Type art = tb.CreateType(); // Create the type
        
          object arobj = Activator.CreateInstance(art, null, null);

          art.GetField("__ArrayRoot", BindingFlags.Public | BindingFlags.Instance).SetValue(arobj, n.AryVal.ToArray());

          return n.AryVal.ToArray();
        }

        First = false;

        if (n.IsObj) {
          TypeBuilder objtb = StartType("OBJTYPE_" + n.Name);

          Type objt = ParseObj(objtb, n.Value.ToJsonList());
         
          tb.DefineField(n.Name, objt, FieldAttributes.Public);
          continue;
        }

        if (n.IsAry) {
          tb.DefineField(n.Name, typeof(string[]), FieldAttributes.Public);
          continue;
        }

        tb.DefineField(n.Name, typeof(string), FieldAttributes.Public);
      }

      // Create the type
      Type t = tb.CreateType();

      // Instantiate
      object obj = Activator.CreateInstance(t, null, null);

      foreach (Node n in js.ToJsonList()) {

        if (n.IsObj) {

          // Get the object field
          FieldInfo of = t.GetField(n.Name, BindingFlags.Public | BindingFlags.Instance);

          object o = CreateObj(of.FieldType, Json.GetList(n.Value));

          // Set it and continue
          t.GetField(n.Name, BindingFlags.Public | BindingFlags.Instance).SetValue(obj, o);
          continue;
        }

        // Add an array
        if (n.IsAry) {
          List<string> ary = new List<string>();

          foreach (string val in n.AryVal) {
            ary.Add(val);
          }

          t.GetField(n.Name, BindingFlags.Public | BindingFlags.Instance).SetValue(obj, ary.ToArray());
          continue;
        }

        t.GetField(n.Name, BindingFlags.Public | BindingFlags.Instance).SetValue(obj, n.Value);
      }

      return obj;
    }

    #region Serialization

    internal static string PrimitiveConvert(object o) {
      if (o is string) {
        return '"' + (string) o + '"';
      }

      if (o is int) {
        return o.ToString();
      }

      return Stringify(o);
    }

    internal static string ConvertField(object obj, FieldInfo f) {
      if (f.FieldType == typeof(string)) {
        return '"' + (string) f.GetValue(obj) + '"';
      }

      if (f.FieldType == typeof(int)) {
        return f.GetValue(obj).ToString();
      }

      if (f.FieldType == typeof(float) || f.FieldType == typeof(double)) {
        return f.GetValue(obj).ToString();
      }

      if (f.FieldType.IsArray) {
        string str = "[";
        int ind = 0;
        Array ary = f.GetValue(obj) as Array;
        
        foreach (object o in ary) {
          ind++;
          str += PrimitiveConvert(o);
          
          if (ind != ary.Length) {
            str += ", ";
            continue;
          }
        }

        str += "]";
        return str;
      }
      
      return Stringify(f.GetValue(obj));
    }

    public static string Stringify(object obj) {
      FieldInfo[] fields = obj.GetType().GetFields();

      string json = "{";
      int ind = 0;

      foreach (FieldInfo f in fields) {
        ind++;
        json += $" \"{f.Name}\": ";
        json += ConvertField(obj, f);

        if (ind != fields.Length) {
          json += ", ";
          continue;
        }
      }

      json += " }";
      return json;
    }

    #endregion Serialization

    // C# 3.9 and lower
    public static object GetField(object root, string name) {
      FieldInfo fi = root.GetType().GetField(name, BindingFlags.Public | BindingFlags.Instance);

      if (fi == null) {
        throw new Exception("Object does not have property '" + name + "'");
      }

      return fi.GetValue(root);
    }

    // C# 3.9 and lower
    public static dynamic GetField(object root, params string[] names) {
      FieldInfo fi = root.GetType().GetField(names[0], BindingFlags.Public | BindingFlags.Instance);

      if (fi == null) {
        throw new Exception("Object does not have property '" + names[0] + "'");
      }

      object last = root;

      foreach (string s in names) {
        if (s == null) continue;
        
        FieldInfo ofi = last.GetType().GetField(s, BindingFlags.Public | BindingFlags.Instance);
        
        last = ofi.GetValue(last);

        
      }

      return last;
    }

  }

}