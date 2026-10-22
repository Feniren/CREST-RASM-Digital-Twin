; NAME: Drill grid, plain and pecked
; CATEGORY: Drilling
; TOOLS: T4 = D3 mm drill, 118 degree point
; STOCK: 100 x 100 x 50 mm, X0 Y0 at the front-left corner, Z0 at the top face
; Intelitek ProMill 8000 dialect (CNCBase). G81 with G99 (return to R), G83 pecking with G98 (return to the initial level).
; Four 8 mm holes along the front, four 20 mm pecked holes behind them.
%
N10 G71 G90 G17 G40 G80
N20 T4 M06
N30 S3000 M03
N40 G00 X25 Y25 Z10
N50 G99 G81 X25 Y25 Z-8 R2 F150
N60 X50
N70 X75
N80 X75 Y50
N90 G80
N100 G00 Z10
N110 G98 G83 X25 Y75 Z-20 R2 Q4 F120
N120 X50
N130 X75
N140 X50 Y50
N150 G80
N160 G00 Z10
N170 M05
N180 G28
N190 M30
%
