public class UnitReferences 
{
    public string unitID;
    public Unit data;
    public UnitInfo info;

    public UnitReferences() { }

    public UnitReferences(Unit unit)
    {
        this.unitID = unit.unitID;
        this.data = unit; 
    }
}
