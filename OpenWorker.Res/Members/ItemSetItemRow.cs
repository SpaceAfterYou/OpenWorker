namespace OpenWorker.UpdateContent.Res.Rows;

/// <summary>
/// What a row of <c>tb_Item_SetItem</c> gives beyond its columns, which are generated from <c>Structures.json</c>.
/// </summary>
public readonly partial struct ItemSetItemRow
{
    /// <summary>
    /// One of the six pieces of the set, from 0; 0 is none.
    /// </summary>
    public int Piece(int index)
    {
        return index switch
        {
            0 => Item1,
            1 => Item2,
            2 => Item3,
            3 => Item4,
            4 => Item5,
            5 => Item6,
            _ => 0
        };
    }

    /// <summary>
    /// The four effects the set gives when that many of its pieces are worn, from two to six: the class of each (0 works on a
    /// stat), its effect and its value. An effect of 0 is none.
    /// </summary>
    public (byte Kind, int Type, float Value)[] Bonus(int pieces)
    {
        return pieces switch
        {
            2 => [(FirstClassOf2, FirstTypeOf2, FirstValueOf2), (SecondClassOf2, SecondTypeOf2, SecondValueOf2), (ThirdClassOf2, ThirdTypeOf2, ThirdValueOf2), (FourthClassOf2, FourthTypeOf2, FourthValueOf2)],
            3 => [(FirstClassOf3, FirstTypeOf3, FirstValueOf3), (SecondClassOf3, SecondTypeOf3, SecondValueOf3), (ThirdClassOf3, ThirdTypeOf3, ThirdValueOf3), (FourthClassOf3, FourthTypeOf3, FourthValueOf3)],
            4 => [(FirstClassOf4, FirstTypeOf4, FirstValueOf4), (SecondClassOf4, SecondTypeOf4, SecondValueOf4), (ThirdClassOf4, ThirdTypeOf4, ThirdValueOf4), (FourthClassOf4, FourthTypeOf4, FourthValueOf4)],
            5 => [(FirstClassOf5, FirstTypeOf5, FirstValueOf5), (SecondClassOf5, SecondTypeOf5, SecondValueOf5), (ThirdClassOf5, ThirdTypeOf5, ThirdValueOf5), (FourthClassOf5, FourthTypeOf5, FourthValueOf5)],
            6 => [(FirstClassOf6, FirstTypeOf6, FirstValueOf6), (SecondClassOf6, SecondTypeOf6, SecondValueOf6), (ThirdClassOf6, ThirdTypeOf6, ThirdValueOf6), (FourthClassOf6, FourthTypeOf6, FourthValueOf6)],
            _ => []
        };
    }
}
