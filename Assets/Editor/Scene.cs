#region Author
/************************************************************************************************************
Author: Nidre (Erdin Kacan)
Website: http://erdinkacan.tumblr.com/
GitHub: https://github.com/Nidre
Behance : https://www.behance.net/erdinkacan
************************************************************************************************************/
#endregion
#region Copyright
/************************************************************************************************************
The MIT License (MIT)
Copyright (c) 2015 Erdin
Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:
The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.
THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
************************************************************************************************************/
#endregion

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