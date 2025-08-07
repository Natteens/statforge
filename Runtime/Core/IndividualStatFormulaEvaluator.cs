using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

namespace StatForge
{
    /// <summary>
    /// Enhanced formula evaluator for individual Stat objects.
    /// Supports complex formulas with stat references and mathematical operations.
    /// </summary>
    public static class IndividualStatFormulaEvaluator
    {
        private const string STAT_PATTERN = @"\b([A-Za-z][A-Za-z0-9_]*)\b";
        private const string PERCENTAGE_PATTERN = @"(\d+(?:\.\d+)?)%\s*([A-Za-z][A-Za-z0-9_]*)";
        private const string FUNCTION_PATTERN = @"\b(min|max|abs|floor|ceil|round|sqrt|sin|cos|tan)\s*\(([^)]+)\)";
        private const float PERCENTAGE_DIVISOR = 100f;
        
        /// <summary>
        /// Evaluates a formula for an individual stat.
        /// </summary>
        public static float Evaluate(string formula, Stat currentStat, StatCollection collection, GameObject owner)
        {
            if (string.IsNullOrEmpty(formula))
                return 0f;
            
            try
            {
                formula = PreProcessFormula(formula);
                var processedFormula = ProcessFormula(formula, currentStat, collection, owner);
                return EvaluateMathExpression(processedFormula);
            }
            catch (Exception e)
            {
                Debug.LogError($"Formula evaluation error for stat '{currentStat?.Name}': {e.Message}. Formula: {formula}");
                return 0f;
            }
        }
        
        /// <summary>
        /// Pre-processes the formula by normalizing whitespace and operators.
        /// </summary>
        public static string PreProcessFormula(string formula)
        {
            if (string.IsNullOrEmpty(formula))
                return string.Empty;
            
            // Normalize whitespace around operators
            formula = Regex.Replace(formula, @"([\+\-\*\/\(\)])", " $1 ");
            formula = Regex.Replace(formula, @"\s+", " ");
            return formula.Trim();
        }
        
        private static string ProcessFormula(string formula, Stat currentStat, StatCollection collection, GameObject owner)
        {
            // Process mathematical functions first
            formula = ProcessMathFunctions(formula);
            
            // Process percentage references
            formula = ProcessPercentageReferences(formula, currentStat, collection, owner);
            
            // Process stat references
            formula = ProcessStatReferences(formula, currentStat, collection, owner);
            
            return formula;
        }
        
        private static string ProcessMathFunctions(string formula)
        {
            return Regex.Replace(formula, FUNCTION_PATTERN, match =>
            {
                var functionName = match.Groups[1].Value.ToLower();
                var arguments = match.Groups[2].Value;
                
                try
                {
                    var argValue = EvaluateMathExpression(arguments);
                    
                    return functionName switch
                    {
                        "min" => HandleMinMax(arguments, true).ToString("F6"),
                        "max" => HandleMinMax(arguments, false).ToString("F6"),
                        "abs" => Mathf.Abs(argValue).ToString("F6"),
                        "floor" => Mathf.Floor(argValue).ToString("F6"),
                        "ceil" => Mathf.Ceil(argValue).ToString("F6"),
                        "round" => Mathf.Round(argValue).ToString("F6"),
                        "sqrt" => Mathf.Sqrt(argValue).ToString("F6"),
                        "sin" => Mathf.Sin(argValue * Mathf.Deg2Rad).ToString("F6"),
                        "cos" => Mathf.Cos(argValue * Mathf.Deg2Rad).ToString("F6"),
                        "tan" => Mathf.Tan(argValue * Mathf.Deg2Rad).ToString("F6"),
                        _ => match.Value
                    };
                }
                catch
                {
                    return match.Value;
                }
            });
        }
        
        private static float HandleMinMax(string arguments, bool isMin)
        {
            var args = SplitArguments(arguments);
            if (args.Length == 0) return 0f;
            
            var values = new List<float>();
            foreach (var arg in args)
            {
                if (float.TryParse(arg.Trim(), out float value))
                {
                    values.Add(value);
                }
            }
            
            if (values.Count == 0) return 0f;
            
            return isMin ? values.Min() : values.Max();
        }
        
        private static string[] SplitArguments(string arguments)
        {
            var args = new List<string>();
            var current = "";
            var parenthesesLevel = 0;
            
            foreach (char c in arguments)
            {
                if (c == ',' && parenthesesLevel == 0)
                {
                    args.Add(current);
                    current = "";
                }
                else
                {
                    if (c == '(') parenthesesLevel++;
                    else if (c == ')') parenthesesLevel--;
                    current += c;
                }
            }
            
            if (!string.IsNullOrEmpty(current))
                args.Add(current);
            
            return args.ToArray();
        }
        
        private static string ProcessPercentageReferences(string formula, Stat currentStat, StatCollection collection, GameObject owner)
        {
            return Regex.Replace(formula, PERCENTAGE_PATTERN, match =>
            {
                var percentageValue = match.Groups[1].Value;
                var statName = match.Groups[2].Value;
                
                var statValue = GetStatValue(statName, currentStat, collection, owner);
                if (float.TryParse(percentageValue, out float percentage))
                {
                    var result = statValue * (percentage / PERCENTAGE_DIVISOR);
                    return result.ToString("F6");
                }
                
                return match.Value;
            });
        }
        
        private static string ProcessStatReferences(string formula, Stat currentStat, StatCollection collection, GameObject owner)
        {
            return Regex.Replace(formula, STAT_PATTERN, match =>
            {
                var statName = match.Groups[1].Value;
                
                // Skip mathematical keywords and numbers
                if (IsMathKeyword(statName) || float.TryParse(statName, out _))
                {
                    return match.Value;
                }
                
                var statValue = GetStatValue(statName, currentStat, collection, owner);
                return statValue.ToString("F6");
            });
        }
        
        private static bool IsMathKeyword(string word)
        {
            return word.ToLower() switch
            {
                "min" or "max" or "abs" or "floor" or "ceil" or "round" or
                "sqrt" or "sin" or "cos" or "tan" or "pi" or "e" => true,
                _ => false
            };
        }
        
        private static float GetStatValue(string statName, Stat currentStat, StatCollection collection, GameObject owner)
        {
            // Prevent self-reference
            if (currentStat != null && currentStat.Name.Equals(statName, StringComparison.OrdinalIgnoreCase))
            {
                return currentStat.BaseValue; // Use base value to prevent circular reference
            }
            
            // Try to get from parent collection first
            if (collection != null)
            {
                var value = collection.Get(statName);
                if (value != 0f || collection.HasStat(statName))
                {
                    return value;
                }
            }
            
            // Try to get from owner GameObject if available
            if (owner != null)
            {
                return owner.GetStat(statName);
            }
            
            // Try to find [Stat] fields in the owner's components
            if (owner != null)
            {
                var components = owner.GetComponents<MonoBehaviour>();
                foreach (var component in components)
                {
                    if (component == null) continue;
                    
                    var fields = component.GetType().GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    foreach (var field in fields)
                    {
                        if (field.FieldType == typeof(Stat))
                        {
                            var stat = field.GetValue(component) as Stat;
                            if (stat != null && stat.Name.Equals(statName, StringComparison.OrdinalIgnoreCase))
                            {
                                return stat.Value;
                            }
                        }
                        
                        // Check for [Stat] attributes on primitive fields
                        var statAttr = System.Attribute.GetCustomAttribute(field, typeof(StatAttribute)) as StatAttribute;
                        if (statAttr != null)
                        {
                            var fieldStatName = !string.IsNullOrEmpty(statAttr.Name) ? statAttr.Name : field.Name;
                            if (fieldStatName.Equals(statName, StringComparison.OrdinalIgnoreCase))
                            {
                                if (field.FieldType == typeof(float))
                                {
                                    return (float)field.GetValue(component);
                                }
                                else if (field.FieldType == typeof(int))
                                {
                                    return (int)field.GetValue(component);
                                }
                                else if (field.FieldType == typeof(Stat))
                                {
                                    var stat = field.GetValue(component) as Stat;
                                    return stat?.Value ?? 0f;
                                }
                            }
                        }
                    }
                }
            }
            
            Debug.LogWarning($"Stat '{statName}' not found for formula evaluation. Returning 0.");
            return 0f;
        }
        
        private static float EvaluateMathExpression(string expression)
        {
            try
            {
                // Use System.Data.DataTable for complex expression evaluation
                var dataTable = new System.Data.DataTable();
                var result = dataTable.Compute(expression, null);
                return Convert.ToSingle(result);
            }
            catch
            {
                // Fallback to basic math parser
                return EvaluateBasicMath(expression);
            }
        }
        
        private static float EvaluateBasicMath(string expression)
        {
            expression = expression.Replace(" ", "");
            
            if (string.IsNullOrEmpty(expression))
                return 0f;
            
            // Handle parentheses first
            while (expression.Contains("("))
            {
                var openIndex = expression.LastIndexOf('(');
                var closeIndex = expression.IndexOf(')', openIndex);
                
                if (closeIndex == -1)
                    throw new ArgumentException("Mismatched parentheses");
                
                var innerExpression = expression.Substring(openIndex + 1, closeIndex - openIndex - 1);
                var innerResult = EvaluateBasicMath(innerExpression);
                
                expression = expression.Substring(0, openIndex) + 
                           innerResult.ToString("F6") + 
                           expression.Substring(closeIndex + 1);
            }
            
            return EvaluateAdditionSubtraction(expression);
        }
        
        private static float EvaluateAdditionSubtraction(string expression)
        {
            var parts = SplitByOperators(expression, '+', '-');
            
            if (parts.Length == 1)
                return EvaluateMultiplicationDivision(parts[0]);
            
            float result = EvaluateMultiplicationDivision(parts[0]);
            
            for (int i = 1; i < parts.Length; i++)
            {
                var operatorIndex = FindOperatorIndex(expression, parts, i);
                var operatorChar = expression[operatorIndex];
                var value = EvaluateMultiplicationDivision(parts[i]);
                
                if (operatorChar == '+')
                    result += value;
                else if (operatorChar == '-')
                    result -= value;
            }
            
            return result;
        }
        
        private static float EvaluateMultiplicationDivision(string expression)
        {
            var parts = SplitByOperators(expression, '*', '/');
            
            if (parts.Length == 1)
                return ParseFloat(parts[0]);
            
            float result = ParseFloat(parts[0]);
            
            for (int i = 1; i < parts.Length; i++)
            {
                var operatorIndex = FindOperatorIndex(expression, parts, i);
                var operatorChar = expression[operatorIndex];
                var value = ParseFloat(parts[i]);
                
                if (operatorChar == '*')
                    result *= value;
                else if (operatorChar == '/')
                {
                    if (Mathf.Approximately(value, 0f))
                        throw new DivideByZeroException("Division by zero in formula");
                    result /= value;
                }
            }
            
            return result;
        }
        
        private static string[] SplitByOperators(string expression, params char[] operators)
        {
            var parts = new List<string>();
            var currentPart = "";
            
            for (int i = 0; i < expression.Length; i++)
            {
                var character = expression[i];
                
                if (operators.Contains(character) && i > 0)
                {
                    parts.Add(currentPart);
                    currentPart = "";
                }
                else
                {
                    currentPart += character;
                }
            }
            
            if (!string.IsNullOrEmpty(currentPart))
                parts.Add(currentPart);
            
            return parts.ToArray();
        }
        
        private static int FindOperatorIndex(string expression, string[] parts, int partIndex)
        {
            int currentIndex = 0;
            
            for (int i = 0; i < partIndex; i++)
            {
                currentIndex += parts[i].Length;
                if (i < partIndex - 1)
                    currentIndex++;
            }
            
            return currentIndex;
        }
        
        private static float ParseFloat(string value)
        {
            if (float.TryParse(value.Trim(), out float result))
                return result;
                
            throw new ArgumentException($"Cannot parse '{value}' as a number");
        }
        
        /// <summary>
        /// Validates if a formula is syntactically correct.
        /// </summary>
        public static bool ValidateFormula(string formula)
        {
            if (string.IsNullOrEmpty(formula))
                return true;
            
            try
            {
                // Basic syntax validation
                formula = PreProcessFormula(formula);
                
                // Check for balanced parentheses
                int parenthesesCount = 0;
                foreach (char c in formula)
                {
                    if (c == '(') parenthesesCount++;
                    else if (c == ')') parenthesesCount--;
                    
                    if (parenthesesCount < 0) return false;
                }
                
                if (parenthesesCount != 0) return false;
                
                // Try to evaluate with dummy values
                var dummyFormula = Regex.Replace(formula, STAT_PATTERN, match =>
                {
                    var statName = match.Groups[1].Value;
                    return IsMathKeyword(statName) ? match.Value : "1.0";
                });
                
                EvaluateMathExpression(dummyFormula);
                return true;
            }
            catch
            {
                return false;
            }
        }
        
        /// <summary>
        /// Extracts all stat references from a formula.
        /// </summary>
        public static string[] ExtractStatReferences(string formula)
        {
            if (string.IsNullOrEmpty(formula))
                return new string[0];
            
            var matches = Regex.Matches(formula, STAT_PATTERN);
            return matches.Cast<Match>()
                         .Select(m => m.Groups[1].Value)
                         .Where(name => !IsMathKeyword(name) && !float.TryParse(name, out _))
                         .Distinct()
                         .ToArray();
        }
    }
}