; NAME: Rectangular pocket
; CATEGORY: Pocketing
; TOOLS: T1 = D10 mm flat end mill
; STOCK: 100 x 100 x 50 mm, X0 Y0 at the front-left corner, Z0 at the top face
; Intelitek ProMill 8000 dialect (CNCBase). Semicolon comments, G71 metric, F in mm/min, G04 F = seconds.
; M06 tool changes are a demo extension. Pocket 60 x 40 centred at (50, 50), 3 mm deep, zig-zag + wall pass.
%
N10 G71 G90 G17 G40 G80
N20 T1 M06
N30 S2500 M03
N40 G00 X25 Y35 Z10
N50 G01 Z-3 F150
N60 X75 F500
N70 Y42
N80 X25
N90 Y49
N100 X75
N110 Y56
N120 X25
N130 Y63
N140 X75
N150 Y65
N160 X25
N170 Y35
N180 X75
N190 Y65
N200 X25
N210 Y35
N220 G00 Z10
N230 M05
N240 G28
N250 M30
%
