# Downloaded from http://www.hevanet.com/cristofd/brainfuck/sierpinski.b

# sierpinski.b -- display Sierpinski triangle
# (c) 2016 Daniel B. Cristofani
# http://brainfuck.org/

++++++++[>+>++++<<-]>++>>+<[-[>>+<<-]+>>]>+[
    -<<<[
        ->[+[-]+>++>>>-<<]<[<]>>++++++[<<+++++>>-]+<<++.[-]<<
    ]>.>+[>>]>+
]

# Shows an ASCII representation of the Sierpinski triangle
# (iteration 5).
