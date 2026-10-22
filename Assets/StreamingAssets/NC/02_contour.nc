; NAME: Rounded profile, ring groove and an R arc
; CATEGORY: Contouring
; TOOLS: T2 = D6 mm flat end mill
; STOCK: 100 x 100 x 50 mm, X0 Y0 at the front-left corner, Z0 at the top face
; Intelitek ProMill 8000 dialect (CNCBase). Arcs use I/J incremental from the start point, one uses R.
; Rounded rectangle 70 x 50 with R10 corners at Z-3, a full circle R15 at Z-2, a smile arc R20 at Z-2.
%
N10 G71 G90 G17 G40 G80
N20 T2 M06
N30 S3000 M03
N40 G00 X25 Y25 Z10
N50 G01 Z-3 F150
N60 X75 F400
N70 G03 X85 Y35 I0 J10
N80 G01 Y65
N90 G03 X75 Y75 I-10 J0
N100 G01 X25
N110 G03 X15 Y65 I0 J-10
N120 G01 Y35
N130 G03 X25 Y25 I10 J0
N140 G00 Z10
N150 X65 Y50
N160 G01 Z-2 F150
N170 G02 X65 Y50 I-15 J0 F400
N180 G00 Z10
N190 X35 Y90
N200 G01 Z-2 F150
N210 G03 X65 Y90 R20 F400
N220 G00 Z10
N230 M05
N240 G28
N250 M30
%
