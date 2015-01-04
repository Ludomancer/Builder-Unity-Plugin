using System;

// Holds scene information
[Serializable]
class Scene
{
    public Scene(string path)
    {
        Path = path;
    }

    internal int Id;
    private string _path;

    public string Path
    {
        get { return _path; }
        set
        {
            _path = value;
            _name = System.IO.Path.GetFileNameWithoutExtension(value);
            Id = _path.GetHashCode();
        }
    }
    private string _name;

    public string Name
    {
        get { return _name; }
    }
    public bool isFound = true;

    public override bool Equals(object other)
    {
        return Equals(other as Scene);
    }

    public virtual bool Equals(Scene other)
    {
        if (other == null) { return false; }
        if (object.ReferenceEquals(this, other)) { return true; }
        return this.Id == other.Id;
    }

    public override int GetHashCode()
    {
        return this.Id;
    }

    public static bool operator ==(Scene item1, Scene item2)
    {
        if (object.ReferenceEquals(item1, item2)) { return true; }
        if ((object)item1 == null || (object)item2 == null) { return false; }
        return item1.Id == item2.Id;
    }

    public static bool operator !=(Scene item1, Scene item2)
    {
        return !(item1 == item2);
    }

}