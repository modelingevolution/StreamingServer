namespace ModelingEvolution.IO.Nal;


public enum NALType : byte
{
    /// <summary>
    /// Coded slice of an IDR picture (I-frame), Type 5
    /// </summary>
     IFrame = 0x65,
    /// <summary>
    /// Coded slice of a non-IDR picture (P-frame), Type 1
    /// </summary>
     PFrame = 0x41,

    /// <summary>
    /// Sequence parameter set (B-frame), Type 7 | Supplemental Enhancement Information
    /// </summary>
     SPS = 0x27,

    
    /// <summary>
    /// Picture parameter set (B-frame), Type 8 | Video Coding Layer
    /// </summary>
     PPS = 0x28,

    /// <summary>
    /// Coded slice of an IDR picture (B-frame), Type 5
    /// </summary>
     IDRBFrame = 0x25,

    /// <summary>
    /// Coded slice of a non-IDR picture (B-frame), Type 1
    /// </summary>
     NIDRBFrame = 0x21,

    /// <summary>
    /// NAL unit type 0x37 is an Audible Reference Point NAL unit. It is used to mark a point in the stream where the audio and video should be synchronized. This is typically used to ensure that audio and video playback is in sync when the stream is played back.
    /// </summary>
     ARP = 0x37,
}