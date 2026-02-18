using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Terraria;

namespace ModKitTML
{
    /// <summary>
    /// Reflection-based C# expression evaluator for tModLoader.
    /// Supports assignments and property/field reads on any accessible object.
    /// 
    /// Since tModLoader's build system doesn't support NuGet (Microsoft.CodeAnalysis.CSharp.Scripting),
    /// this evaluator parses expressions manually and executes them via System.Reflection.
    /// 
    /// Supported syntax:
    ///   Main.LocalPlayer.HeldItem.damage = 69;
    ///   Main.LocalPlayer.HeldItem.stack = 77;
    ///   Main.LocalPlayer.HeldItem.defense = 42;
    ///   Main.LocalPlayer.HeldItem.potion = false;
    ///   Main.LocalPlayer.HeldItem.consumable = false;
    ///   Main.LocalPlayer.HeldItem.useTime = 17;
    ///   Main.LocalPlayer.HeldItem.useAnimation = 2;
    ///   Main.LocalPlayer.HeldItem.scale = 1.5f;
    ///   Main.LocalPlayer.HeldItem.prefix = 81;
    ///   Main.LocalPlayer.HeldItem.type = 4;
    ///   Main.dayTime = true;
    ///   Main.time = 27000;
    ///   Main.LocalPlayer.statLife (read)
    ///   Main.LocalPlayer.creativeGodMode = true;
    /// </summary>
    public class REPLBackend
    {
        // Known root types for resolving the first segment of an expression
        private static readonly Dictionary<string, Type> KnownRoots = new()
        {
            ["Main"] = typeof(Main),
            ["NPC"] = typeof(Terraria.NPC),
            ["Item"] = typeof(Terraria.Item),
            ["Player"] = typeof(Terraria.Player),
            ["WorldGen"] = typeof(Terraria.WorldGen),
            ["Projectile"] = typeof(Terraria.Projectile),
            ["NetMessage"] = typeof(Terraria.NetMessage),
            ["Netplay"] = typeof(Terraria.Netplay),
            ["Lighting"] = typeof(Terraria.Lighting),
        };

        // Store variables defined in the REPL session
        private readonly Dictionary<string, object> variables = new();

        public void Reset()
        {
            variables.Clear();
        }

        /// <summary>
        /// Execute a line of C#-like code.
        /// Returns (output, isError).
        /// </summary>
        public (string output, bool isError) Execute(string line)
        {
            if (string.IsNullOrWhiteSpace(line))
                return (null, false);

            line = line.Trim().TrimEnd(';').Trim();

            try
            {
                // Variable assignment: var x = ...  or  int x = ...
                var varAssignMatch = Regex.Match(line, @"^(?:var|int|float|double|bool|string|byte|short|long)\s+(\w+)\s*=\s*(.+)$");
                if (varAssignMatch.Success)
                {
                    string varName = varAssignMatch.Groups[1].Value;
                    string valueExpr = varAssignMatch.Groups[2].Value;
                    object val = EvaluateExpression(valueExpr);
                    variables[varName] = val;
                    return ($"{varName} = {FormatValue(val)}", false);
                }

                // Assignment: path = value
                int eqIndex = FindAssignmentOperator(line);
                if (eqIndex > 0)
                {
                    string leftSide = line[..eqIndex].Trim();
                    string rightSide = line[(eqIndex + 1)..].Trim();

                    object value = EvaluateExpression(rightSide);
                    SetMember(leftSide, value);
                    // Read back and confirm
                    object readBack;
                    try { readBack = ResolvePath(leftSide); }
                    catch { readBack = value; }
                    return ($"{leftSide} = {FormatValue(readBack)}", false);
                }

                // Method call: Main.StartRain() or Main.StopRain()
                if (line.EndsWith("()"))
                {
                    string methodPath = line[..^2];
                    InvokeMethod(methodPath, Array.Empty<object>());
                    return ($"Called {line}", false);
                }

                // Method call with args: NPC.NewNPC(null, 100, 200, 50)
                var methodMatch = Regex.Match(line, @"^(.+?)\((.+)\)$");
                if (methodMatch.Success && !line.Contains("="))
                {
                    string methodPath = methodMatch.Groups[1].Value;
                    string argsStr = methodMatch.Groups[2].Value;
                    object[] args = ParseArguments(argsStr);
                    object result = InvokeMethod(methodPath, args);
                    return (result != null ? $"=> {FormatValue(result)}" : $"Called {methodPath}(...)", false);
                }

                // Read expression
                object val2 = EvaluateExpression(line);
                return ($"=> {FormatValue(val2)}", false);
            }
            catch (Exception e)
            {
                string msg = e.InnerException?.Message ?? e.Message;
                return ($"Error: {msg}", true);
            }
        }

        private object EvaluateExpression(string expr)
        {
            expr = expr.Trim();

            // null
            if (expr == "null") return null;

            // bool
            if (expr == "true") return true;
            if (expr == "false") return false;

            // float literal (ends with f)
            if (expr.EndsWith("f", StringComparison.OrdinalIgnoreCase) &&
                float.TryParse(expr[..^1], NumberStyles.Float, CultureInfo.InvariantCulture, out float fVal))
                return fVal;

            // double literal (ends with d or contains .)
            if (expr.EndsWith("d", StringComparison.OrdinalIgnoreCase) &&
                double.TryParse(expr[..^1], NumberStyles.Float, CultureInfo.InvariantCulture, out double dVal))
                return dVal;

            // int literal
            if (int.TryParse(expr, out int iVal))
                return iVal;

            // double with decimal point (no suffix)
            if (expr.Contains('.') &&
                double.TryParse(expr, NumberStyles.Float, CultureInfo.InvariantCulture, out double dVal2))
                return dVal2;

            // long literal
            if (expr.EndsWith("L", StringComparison.OrdinalIgnoreCase) &&
                long.TryParse(expr[..^1], out long lVal))
                return lVal;

            // string literal
            if ((expr.StartsWith("\"") && expr.EndsWith("\"")) ||
                (expr.StartsWith("'") && expr.EndsWith("'")))
                return expr[1..^1];

            // byte cast: (byte)123
            var castMatch = Regex.Match(expr, @"^\(byte\)\s*(.+)$");
            if (castMatch.Success)
            {
                object inner = EvaluateExpression(castMatch.Groups[1].Value);
                return Convert.ToByte(inner);
            }

            // Negative number
            if (expr.StartsWith("-"))
            {
                string rest = expr[1..];
                if (rest.EndsWith("f", StringComparison.OrdinalIgnoreCase) &&
                    float.TryParse(rest[..^1], NumberStyles.Float, CultureInfo.InvariantCulture, out float nf))
                    return -nf;
                if (int.TryParse(rest, out int ni))
                    return -ni;
                if (double.TryParse(rest, NumberStyles.Float, CultureInfo.InvariantCulture, out double nd))
                    return -nd;
            }

            // Method call expression: Something.Method()
            if (expr.EndsWith("()"))
            {
                string methodPath = expr[..^2];
                return InvokeMethod(methodPath, Array.Empty<object>());
            }

            var mCall = Regex.Match(expr, @"^(.+?)\((.+)\)$");
            if (mCall.Success)
            {
                string methodPath = mCall.Groups[1].Value;
                string argsStr = mCall.Groups[2].Value;
                object[] args = ParseArguments(argsStr);
                return InvokeMethod(methodPath, args);
            }

            // Check REPL variables
            if (variables.TryGetValue(expr, out object varVal))
                return varVal;

            // Resolve member path: Main.LocalPlayer.statLife etc
            return ResolvePath(expr);
        }

        private object ResolvePath(string path)
        {
            string[] parts = SplitPath(path);
            if (parts.Length == 0)
                throw new Exception($"Empty expression");

            // Resolve root
            string rootName = parts[0];
            object current = null;
            Type currentType;
            bool isStatic;

            if (variables.TryGetValue(rootName, out object varObj))
            {
                current = varObj;
                currentType = current?.GetType() ?? typeof(object);
                isStatic = false;
            }
            else if (KnownRoots.TryGetValue(rootName, out Type rootType))
            {
                currentType = rootType;
                isStatic = true;
            }
            else
            {
                throw new Exception($"Unknown root: '{rootName}'. Known: {string.Join(", ", KnownRoots.Keys)}");
            }

            // Walk the rest of the path
            for (int i = 1; i < parts.Length; i++)
            {
                string member = parts[i];
                var flags = BindingFlags.Public | BindingFlags.NonPublic |
                            (isStatic ? BindingFlags.Static : BindingFlags.Instance);

                // Try property
                var prop = currentType.GetProperty(member, flags);
                if (prop != null)
                {
                    current = prop.GetValue(isStatic ? null : current);
                    currentType = prop.PropertyType;
                    isStatic = false;
                    continue;
                }

                // Try field
                var field = currentType.GetField(member, flags);
                if (field != null)
                {
                    current = field.GetValue(isStatic ? null : current);
                    currentType = field.FieldType;
                    isStatic = false;
                    continue;
                }

                throw new Exception($"Member '{member}' not found on {currentType.Name}");
            }

            return current;
        }

        private void SetMember(string path, object value)
        {
            string[] parts = SplitPath(path);
            if (parts.Length < 2)
                throw new Exception("Cannot assign to a root object directly");

            // Resolve up to the parent
            string parentPath = string.Join(".", parts.Take(parts.Length - 1));
            string memberName = parts.Last();

            object parent = ResolvePath(parentPath);
            if (parent == null)
                throw new Exception($"Parent object '{parentPath}' is null");

            Type parentType = parent.GetType();
            var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

            // Try field first (most Terraria members are fields)
            var field = parentType.GetField(memberName, flags);
            if (field != null)
            {
                object converted = ConvertValue(value, field.FieldType);
                field.SetValue(parent, converted);
                return;
            }

            // Try property
            var prop = parentType.GetProperty(memberName, flags);
            if (prop != null && prop.CanWrite)
            {
                object converted = ConvertValue(value, prop.PropertyType);
                prop.SetValue(parent, converted);
                return;
            }

            // Try static
            var sField = parentType.GetField(memberName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            if (sField != null)
            {
                object converted = ConvertValue(value, sField.FieldType);
                sField.SetValue(null, converted);
                return;
            }

            // Edge case: the parent path might itself be a static type
            // e.g., Main.dayTime where we resolve "Main" as type, not instance
            string rootName = parts[0];
            if (KnownRoots.TryGetValue(rootName, out Type rootType) && parts.Length == 2)
            {
                var sf = rootType.GetField(memberName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                if (sf != null)
                {
                    sf.SetValue(null, ConvertValue(value, sf.FieldType));
                    return;
                }
                var sp = rootType.GetProperty(memberName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                if (sp != null && sp.CanWrite)
                {
                    sp.SetValue(null, ConvertValue(value, sp.PropertyType));
                    return;
                }
            }

            throw new Exception($"Cannot set '{memberName}' on {parentType.Name} — field/property not found or read-only");
        }

        private object InvokeMethod(string methodPath, object[] args)
        {
            string[] parts = SplitPath(methodPath);
            if (parts.Length < 2)
                throw new Exception("Method path too short");

            string parentPath = string.Join(".", parts.Take(parts.Length - 1));
            string methodName = parts.Last();

            // Resolve parent — could be static root or instance
            object parent = null;
            Type parentType;
            bool isStatic = false;

            if (parts.Length == 2 && KnownRoots.TryGetValue(parts[0], out Type rootType))
            {
                parentType = rootType;
                isStatic = true;
            }
            else
            {
                parent = ResolvePath(parentPath);
                if (parent == null)
                    throw new Exception($"Parent '{parentPath}' is null");
                parentType = parent.GetType();
            }

            var flags = BindingFlags.Public | BindingFlags.NonPublic |
                        (isStatic ? BindingFlags.Static : BindingFlags.Instance);

            // Find methods with matching name
            var methods = parentType.GetMethods(flags).Where(m => m.Name == methodName).ToArray();
            if (methods.Length == 0)
                throw new Exception($"Method '{methodName}' not found on {parentType.Name}");

            // Try to find best match by parameter count
            foreach (var method in methods.OrderBy(m => Math.Abs(m.GetParameters().Length - args.Length)))
            {
                var parameters = method.GetParameters();
                if (parameters.Length != args.Length) continue;

                try
                {
                    object[] convertedArgs = new object[args.Length];
                    for (int i = 0; i < args.Length; i++)
                    {
                        if (args[i] == null)
                            convertedArgs[i] = null;
                        else
                            convertedArgs[i] = ConvertValue(args[i], parameters[i].ParameterType);
                    }
                    return method.Invoke(isStatic ? null : parent, convertedArgs);
                }
                catch { continue; }
            }

            // Fallback: try with null-padded args for methods with optional/more params
            var bestMethod = methods.FirstOrDefault();
            if (bestMethod != null)
            {
                var pars = bestMethod.GetParameters();
                object[] paddedArgs = new object[pars.Length];
                for (int i = 0; i < pars.Length; i++)
                {
                    if (i < args.Length && args[i] != null)
                        paddedArgs[i] = ConvertValue(args[i], pars[i].ParameterType);
                    else if (pars[i].HasDefaultValue)
                        paddedArgs[i] = pars[i].DefaultValue;
                    else
                        paddedArgs[i] = null;
                }
                return bestMethod.Invoke(isStatic ? null : parent, paddedArgs);
            }

            throw new Exception($"No matching overload for '{methodName}' with {args.Length} arguments");
        }

        private object ConvertValue(object value, Type targetType)
        {
            if (value == null) return null;
            if (targetType.IsAssignableFrom(value.GetType())) return value;

            // Handle enum types
            if (targetType.IsEnum)
            {
                if (value is int intVal)
                    return Enum.ToObject(targetType, intVal);
                if (value is string strVal)
                    return Enum.Parse(targetType, strVal);
            }

            return Convert.ChangeType(value, targetType, CultureInfo.InvariantCulture);
        }

        private object[] ParseArguments(string argsStr)
        {
            // Simple comma-split (doesn't handle nested parens/strings with commas)
            var args = new List<object>();
            int depth = 0;
            int start = 0;
            for (int i = 0; i <= argsStr.Length; i++)
            {
                char c = i < argsStr.Length ? argsStr[i] : ',';
                if (c == '(' || c == '[') depth++;
                else if (c == ')' || c == ']') depth--;
                else if (c == ',' && depth == 0 || i == argsStr.Length)
                {
                    string argStr = argsStr[start..i].Trim();
                    args.Add(EvaluateExpression(argStr));
                    start = i + 1;
                }
            }
            return args.ToArray();
        }

        private static string[] SplitPath(string path)
        {
            // Split on '.' but not inside parentheses
            var parts = new List<string>();
            int depth = 0;
            int start = 0;
            for (int i = 0; i < path.Length; i++)
            {
                if (path[i] == '(' || path[i] == '[') depth++;
                else if (path[i] == ')' || path[i] == ']') depth--;
                else if (path[i] == '.' && depth == 0)
                {
                    parts.Add(path[start..i]);
                    start = i + 1;
                }
            }
            parts.Add(path[start..]);
            return parts.ToArray();
        }

        private int FindAssignmentOperator(string line)
        {
            // Find '=' that is NOT '==' and NOT inside parentheses
            int depth = 0;
            for (int i = 0; i < line.Length; i++)
            {
                if (line[i] == '(' || line[i] == '[') depth++;
                else if (line[i] == ')' || line[i] == ']') depth--;
                else if (line[i] == '=' && depth == 0)
                {
                    // Skip '=='
                    if (i + 1 < line.Length && line[i + 1] == '=') { i++; continue; }
                    // Skip '!='
                    if (i > 0 && line[i - 1] == '!') continue;
                    return i;
                }
            }
            return -1;
        }

        private static string FormatValue(object value)
        {
            if (value == null) return "<null>";
            if (value is bool b) return b ? "true" : "false";
            if (value is float f) return f.ToString("G", CultureInfo.InvariantCulture) + "f";
            if (value is double d) return d.ToString("G", CultureInfo.InvariantCulture);
            if (value is string s) return $"\"{s}\"";
            return value.ToString();
        }
    }
}