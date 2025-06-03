procedure BoogieEntry_simple();
implementation BoogieEntry_simple()
{
var tmp0: int;
var tmp1: int;
var tmp2: int;
var tmp3: int;
var tmp4: int;
var tmp5: int;
var tmp6: int;
var tmp7: int;
tmp0 := (1) != (0);
tmp1 := (2) - (10);
tmp2 := (8) >= (tmp1);
tmp3 := (1) * (5);
tmp4 := (2) + (3);
tmp5 := (tmp3) == (tmp4);
tmp6 := ((tmp2) != (0)) && ((tmp5) != (0));
tmp7 := ((tmp0) != (0)) || ((tmp6) != (0));
}


