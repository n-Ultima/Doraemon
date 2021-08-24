using System.ComponentModel.DataAnnotations.Schema;

namespace Doraemon.Data.Models.Moderation
{
    public class ModmailSnippet
    {
        /// <summary>
        /// The ID of the snippet.
        /// </summary>
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        /// <summary>
        /// The name of the snippet.
        /// </summary>
        [Column(TypeName = "citext")]
        public string Name { get; set; }
        /// <summary>
        /// The content of the snippet.
        /// </summary>
        public string Content { get; set; }
    }
}