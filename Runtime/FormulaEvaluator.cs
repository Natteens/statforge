using System;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

namespace StatForge
{
    public static class FormulaEvaluator
    {
        private const string STAT_PATTERN = @"\b([A-Za-z][A-Za-z0-9_]*)\b";
        private const string PERCENTAGE_PATTERN = @"(\d+(?:\.\d+)?)%\s*([A-Za-z][A-Za-z0-9_]*)";
        private const char ADDITION_OPERATOR = '+';
        private const char SUBTRACTION_OPERATOR = '-';
        private const char MULTIPLICATION_OPERATOR = '*';
        private const char DIVISION_OPERATOR = '/';
        private const float PERCENTAGE_DIVISOR = 100f;
        
        public static float Evaluate(string formula, StatContainer container)
        {
            if (string.IsNullOrEmpty(formula) || container == null)
            {
                return 0f;
            }
            
            try
            {
                formula = PreProcessFormula(formula);
                var processedFormula = ProcessFormula(formula, container);
                Debug.Log($"Fórmula processada: {formula} => {processedFormula}");
                var result = EvaluateMathExpression(processedFormula);
                
                return result;
            }
            catch (Exception e)
            {
                Debug.LogError($"Formula evaluation error: {e.Message}. Formula: {formula}");
                return 0f;
            }
        }

        public static string PreProcessFormula(string formula)
        {
            formula = Regex.Replace(formula, @"([\+\-\*\/\(\)])", " $1 ");
            formula = Regex.Replace(formula, @"\s+", " ");
            return formula.Trim();
        }
        
        private static string ProcessFormula(string formula, StatContainer container)
        {
            formula = ProcessPercentageReferences(formula, container);
            formula = ProcessStatReferences(formula, container);
            
            return formula;
        }
        
        private static string ProcessPercentageReferences(string formula, StatContainer container)
        {
            return Regex.Replace(formula, PERCENTAGE_PATTERN, match =>
            {
                var percentageValue = match.Groups[1].Value;
                var statName = match.Groups[2].Value;
        
                var stat = FindStatByShortName(container, statName);
                if (stat != null)
                {
                    var statValue = container.GetStatValue(stat.statType);
                    var percentage = float.Parse(percentageValue);
                    var result = statValue * (percentage / PERCENTAGE_DIVISOR);
                    return result.ToString("F2");
                }
        
                return match.Value;
            });
        }
        
        private static string ProcessStatReferences(string formula, StatContainer container)
        {
            return Regex.Replace(formula, STAT_PATTERN, match =>
            {
                var statName = match.Groups[1].Value;
                var stat = FindStatByShortName(container, statName);
                
                if (stat != null)
                {
                    return container.GetStatValue(stat.statType).ToString("F2");
                }
                
                return match.Value;
            });
        }
        
        private static StatValue FindStatByShortName(StatContainer container, string shortName)
        {
            return container.Stats.FirstOrDefault(s => 
                s.statType != null && 
                string.Equals(s.statType.ShortName, shortName, StringComparison.OrdinalIgnoreCase));
        }
        
        private static float EvaluateMathExpression(string expression)
        {
            try
            {
                var dataTable = new System.Data.DataTable();
                var result = dataTable.Compute(expression, null);
                return Convert.ToSingle(result);
            }
            catch
            {
                return EvaluateBasicMath(expression);
            }
        }
        
        private static float EvaluateBasicMath(string expression)
        {
            expression = expression.Replace(" ", "");
            
            if (string.IsNullOrEmpty(expression))
                return 0f;
            
            while (expression.Contains("("))
            {
                var openIndex = expression.LastIndexOf('(');
                var closeIndex = expression.IndexOf(')', openIndex);
                
                if (closeIndex == -1)
                    throw new ArgumentException("Mismatched parentheses");
                
                var innerExpression = expression.Substring(openIndex + 1, closeIndex - openIndex - 1);
                var innerResult = EvaluateBasicMath(innerExpression);
                
                expression = expression.Substring(0, openIndex) + 
                           innerResult.ToString("F2") + 
                           expression.Substring(closeIndex + 1);
            }
            
            return EvaluateAdditionSubtraction(expression);
        }
        
        private static float EvaluateAdditionSubtraction(string expression)
        {
            var parts = SplitByOperators(expression, ADDITION_OPERATOR, SUBTRACTION_OPERATOR);
            
            if (parts.Length == 1)
                return EvaluateMultiplicationDivision(parts[0]);
            
            float result = EvaluateMultiplicationDivision(parts[0]);
            
            for (int i = 1; i < parts.Length; i++)
            {
                var operatorIndex = FindOperatorIndex(expression, parts, i);
                var operatorChar = expression[operatorIndex];
                var value = EvaluateMultiplicationDivision(parts[i]);
                
                if (operatorChar == ADDITION_OPERATOR)
                    result += value;
                else if (operatorChar == SUBTRACTION_OPERATOR)
                    result -= value;
            }
            
            return result;
        }
        
        private static float EvaluateMultiplicationDivision(string expression)
        {
            var parts = SplitByOperators(expression, MULTIPLICATION_OPERATOR, DIVISION_OPERATOR);
            
            if (parts.Length == 1)
                return ParseFloat(parts[0]);
            
            float result = ParseFloat(parts[0]);
            
            for (int i = 1; i < parts.Length; i++)
            {
                var operatorIndex = FindOperatorIndex(expression, parts, i);
                var operatorChar = expression[operatorIndex];
                var value = ParseFloat(parts[i]);
                
                if (operatorChar == MULTIPLICATION_OPERATOR)
                    result *= value;
                else if (operatorChar == DIVISION_OPERATOR)
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
            var parts = new System.Collections.Generic.List<string>();
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
        
        public static bool ValidateFormula(string formula, StatContainer container)
        {
            if (string.IsNullOrEmpty(formula))
                return true;
                
            try
            {
                Evaluate(formula, container);
                return true;
            }
            catch
            {
                return false;
            }
        }
        
        public static string[] ExtractStatReferences(string formula)
        {
            if (string.IsNullOrEmpty(formula))
                return new string[0];
                
            var matches = Regex.Matches(formula, STAT_PATTERN);
            return matches.Cast<Match>()
                         .Select(m => m.Groups[1].Value)
                         .Distinct()
                         .ToArray();
        }
    }
}