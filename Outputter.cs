using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace CSharpToArduino
{
    class Outputter
    {
        List<SyntaxTrivia> handledTrivia = new List<SyntaxTrivia>();

        List<string> _lines = new List<string>();
        string _current = String.Empty;

        public List<string> Lines { get { return _lines; } }

        public void Add(string text)
        {
            if (text == "\r\n")
            {
                _lines.Add(_current);
                _current = String.Empty;
            }
            else
            {
                _current += text;
            }
        }

        public void HandleLeadingTrivia(SyntaxToken syntaxToken)
        {
            AddTrivia(syntaxToken.LeadingTrivia);
        }

        public void HandleTrailingTrivia(SyntaxToken syntaxToken)
        {
            AddTrivia(syntaxToken.TrailingTrivia);
        }

        public void HandleLeadingTrivia(SyntaxNode syntaxNode)
        {
            AddTrivia(syntaxNode.GetLeadingTrivia());
        }

        public void HandleTrailingTrivia(SyntaxNode syntaxNode)
        {
            AddTrivia(syntaxNode.GetTrailingTrivia());
        }

        private void AddTrivia(SyntaxTriviaList syntaxTriviaList)
        {
            foreach (var trivia in syntaxTriviaList)
            {
                if (!handledTrivia.Contains(trivia))
                {
                    Add(trivia.ToString());
                    handledTrivia.Add(trivia);
                }
                else
                {
                }
            }
        }
    }
}
