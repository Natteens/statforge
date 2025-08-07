#!/usr/bin/env python3
"""
Simple validation script to check if the StatForge classes can be parsed correctly.
This performs basic syntax validation on the C# code.
"""

import os
import re
import sys

def check_file_syntax(filepath):
    """Basic syntax checking for C# files"""
    try:
        with open(filepath, 'r', encoding='utf-8') as f:
            content = f.read()
        
        # Check for basic syntax issues
        errors = []
        
        # Check for balanced braces
        open_braces = content.count('{')
        close_braces = content.count('}')
        if open_braces != close_braces:
            errors.append(f"Unbalanced braces: {open_braces} {{ vs {close_braces} }}")
        
        # Check for balanced parentheses
        open_parens = content.count('(')
        close_parens = content.count(')')
        if open_parens != close_parens:
            errors.append(f"Unbalanced parentheses: {open_parens} ( vs {close_parens} )")
        
        # Check for obvious syntax errors
        if re.search(r'class\s+\w+.*\{.*\}', content, re.DOTALL):
            pass  # Has at least one complete class
        elif 'class ' in content:
            errors.append("Incomplete class definition detected")
        
        # Check for missing using statements where needed
        if 'UnityEngine' in content and 'using UnityEngine;' not in content:
            errors.append("Missing 'using UnityEngine;' directive")
        
        return errors
        
    except Exception as e:
        return [f"Failed to read file: {e}"]

def main():
    script_dir = os.path.dirname(os.path.abspath(__file__))
    
    # Files to validate
    files_to_check = [
        'Runtime/Core/Stat.cs',
        'Runtime/Core/IndividualStatFormulaEvaluator.cs',
        'Runtime/Core/StatExtensions.cs',
        'Runtime/Core/StatAttribute.cs',
        'Tests/Runtime/StatForgeNewAPITests.cs',
        'Editor/StatPropertyDrawer.cs',
        'Samples~/Basic/NewAPIPlayerExample.cs'
    ]
    
    all_good = True
    
    print("🔍 Validating StatForge implementation...")
    print()
    
    for file_path in files_to_check:
        full_path = os.path.join(script_dir, file_path)
        if not os.path.exists(full_path):
            print(f"❌ Missing file: {file_path}")
            all_good = False
            continue
        
        errors = check_file_syntax(full_path)
        if errors:
            print(f"❌ Syntax issues in {file_path}:")
            for error in errors:
                print(f"   - {error}")
            all_good = False
        else:
            print(f"✅ {file_path}")
    
    print()
    
    # Validate core features are present
    print("🧪 Checking core API features...")
    
    # Check Stat class
    stat_file = os.path.join(script_dir, 'Runtime/Core/Stat.cs')
    if os.path.exists(stat_file):
        with open(stat_file, 'r') as f:
            stat_content = f.read()
        
        required_features = [
            ('class Stat', 'Stat class definition'),
            ('public float Value', 'Value property'),
            ('public string Formula', 'Formula property'),
            ('AddModifier', 'Modifier support'),
            ('OnValueChanged', 'Event system'),
            ('implicit operator float', 'Implicit conversion')
        ]
        
        for feature, description in required_features:
            if feature in stat_content:
                print(f"✅ {description}")
            else:
                print(f"❌ Missing {description}")
                all_good = False
    
    print()
    
    if all_good:
        print("🎉 All validation checks passed!")
        print("✨ StatForge new API implementation looks good!")
        return 0
    else:
        print("💥 Some validation checks failed.")
        return 1

if __name__ == '__main__':
    sys.exit(main())