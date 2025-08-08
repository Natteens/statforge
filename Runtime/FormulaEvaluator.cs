using System;
using System.Text.RegularExpressions;
using UnityEngine;

namespace StatForge
{
    public static class FormulaEvaluator
    {
        internal static float Evaluate(string formula, StatRegistry registry)
        {
            if (string.IsNullOrEmpty(formula) || registry == null)
                return 0f;
            
            try
            {
                var processedFormula = ProcessFormula(formula, registry);
                return EvaluateMathExpression(processedFormula);
            }
            catch (Exception e)
            {
                Debug.LogError($"[StatForge] Erro na fórmula '{formula}': {e.Message}");
                return 0f;
            }
        }
        
        public static float Evaluate(string formula, StatContainer container)
        {
            if (string.IsNullOrEmpty(formula) || container == null)
                return 0f;
            
            try
            {
                var processedFormula = ProcessFormula(formula, container);
                return EvaluateMathExpression(processedFormula);
            }
            catch (Exception e)
            {
                Debug.LogError($"[StatForge] Erro na fórmula '{formula}': {e.Message}");
                return 0f;
            }
        }
        
        private static string ProcessFormula(string formula, StatRegistry registry)
        {
            var pattern = @"\b([A-Za-z][A-Za-z0-9_]*)\b";
            
            return Regex.Replace(formula, pattern, match =>
            {
                var statName = match.Groups[1].Value;
                var value = registry.GetStatValue(statName);
                return value.ToString("F0", System.Globalization.CultureInfo.InvariantCulture);
            });
        }
        
        private static string ProcessFormula(string formula, StatContainer container)
        {
            var pattern = @"\b([A-Za-z][A-Za-z0-9_]*)\b";
            
            return Regex.Replace(formula, pattern, match =>
            {
                var statName = match.Groups[1].Value;
                var value = container.GetStatValue(statName);
                return value.ToString("F0", System.Globalization.CultureInfo.InvariantCulture);
            });
        }
        
        private static float EvaluateMathExpression(string expression)
        {
            try
            {
                expression = expression.Replace(" ", "");
                return EvaluateExpression(expression);
            }
            catch (Exception e)
            {
                Debug.LogError($"[StatForge] Erro na expressão matemática '{expression}': {e.Message}");
                return 0f;
            }
        }
        
        private static float EvaluateExpression(string expr)
        {
            if (double.TryParse(expr, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double directValue))
                return (float)directValue;
            
            while (expr.Contains("("))
            {
                var start = expr.LastIndexOf('(');
                var end = expr.IndexOf(')', start);
                if (end == -1) break;
                
                var innerExpr = expr.Substring(start + 1, end - start - 1);
                var innerResult = EvaluateExpression(innerExpr);
                expr = expr.Remove(start, end - start + 1).Insert(start, innerResult.ToString("F0", System.Globalization.CultureInfo.InvariantCulture));
            }
            
            expr = ProcessMultiplicationDivision(expr);
            
            return ProcessAdditionSubtraction(expr);
        }
        
        private static string ProcessMultiplicationDivision(string expr)
        {
            var pattern = @"(\d+(?:\.\d+)?(?:[eE][+-]?\d+)?)\s*([*/])\s*(\d+(?:\.\d+)?(?:[eE][+-]?\d+)?)";
            
            while (Regex.IsMatch(expr, pattern))
            {
                expr = Regex.Replace(expr, pattern, match =>
                {
                    var leftStr = match.Groups[1].Value;
                    var op = match.Groups[2].Value;
                    var rightStr = match.Groups[3].Value;
                    
                    if (!double.TryParse(leftStr, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double left))
                        left = 0;
                    
                    if (!double.TryParse(rightStr, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double right))
                        right = 1;
                    
                    var result = op == "*" ? left * right : (right != 0 ? left / right : 0);
                    
                    return result.ToString("F0", System.Globalization.CultureInfo.InvariantCulture);
                });
            }
            
            return expr;
        }
        
        private static float ProcessAdditionSubtraction(string expr)
        {
            var tokens = new System.Collections.Generic.List<string>();
            var currentToken = "";
            var isNegative = false;
            
            if (expr.StartsWith("-"))
            {
                isNegative = true;
                expr = expr.Substring(1);
            }
            else if (expr.StartsWith("+"))
            {
                expr = expr.Substring(1);
            }
            
            for (int i = 0; i < expr.Length; i++)
            {
                var c = expr[i];
                
                if (c == '+' || c == '-')
                {
                    if (!string.IsNullOrEmpty(currentToken))
                    {
                        tokens.Add(isNegative ? "-" + currentToken : currentToken);
                        currentToken = "";
                    }
                    isNegative = c == '-';
                }
                else
                {
                    currentToken += c;
                }
            }
            
            if (!string.IsNullOrEmpty(currentToken))
            {
                tokens.Add(isNegative ? "-" + currentToken : currentToken);
            }
            
            double result = 0.0;
            foreach (var token in tokens)
            {
                if (double.TryParse(token, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double value))
                {
                    result += value;
                }
            }
            
            return (float)result;
        }
    }
}