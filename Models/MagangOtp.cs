namespace Backend.Models
{
    public class MagangTokenLogModel
    {
        public long Id { get; set; }
        public int SessionNm { get; set; }
        public string Event { get; set; } = string.Empty;
        public string? Niklog { get; set; }
        public int IdUser { get; set; }
        public DateTime Expired { get; set; }
    }

    public class VerifyRequestDto
    {
        public long Id { get; set; }
        public string Niklog { get; set; }
        public int IdUser { get; set; } // Server dihilangkan, diganti id_user
    }
}