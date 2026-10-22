; NAME: Pocket, ball grooves and drilled floor with three tool changes
; CATEGORY: Multi-tool
; TOOLS: T1 = D10 mm flat, T3 = D6 mm ball nose, T4 = D3 mm drill
; STOCK: 100 x 100 x 50 mm, X0 Y0 at the front-left corner, Z0 at the top face
; Intelitek ProMill 8000 dialect (CNCBase). M06 tool changes are a demo extension; the tool tip is
; positioned, so tool length needs no offset here (the real control uses its tool library).
%
N10 G71 G90 G17 G40 G80
N20 T1 M06
N30 S2500 M03
N40 G00 X35 Y40 Z10
N50 G01 Z-4 F150
N60 X65 F500
N70 Y47
N80 X35
N90 Y54
N100 X65
N110 Y60
N120 X35
N130 Y40
N140 X65
N150 Y60
N160 X35
N170 G00 Z10
N180 M05
N190 T3 M06
N200 S3000 M03
N210 G00 X10 Y15 Z10
N220 G01 Z-2.5 F150
N230 X90 F500
N240 G00 Z10
N250 X90 Y85
N260 G01 Z-2.5 F150
N270 X10 F500
N280 G00 Z10
N290 M05
N300 T4 M06
N310 S3000 M03
N320 G00 X40 Y50 Z10
N330 G99 G81 X40 Y50 Z-14 R-2 F150
N340 X50
N350 X60
N360 G80
N370 G00 Z10
N380 M05
N390 G28
N400 M30
%
