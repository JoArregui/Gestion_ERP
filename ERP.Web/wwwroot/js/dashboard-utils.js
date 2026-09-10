// Exportación a Excel usando SheetJS (xlsx.full.min.js)
window.exportToExcel = (data, fileName) => {
    const worksheet = XLSX.utils.json_to_sheet(data);
    const workbook = XLSX.utils.book_new();
    XLSX.utils.book_append_sheet(workbook, worksheet, "Reporte");
    XLSX.writeFile(workbook, `${fileName}.xlsx`);
}

// Exportación a PDF usando jsPDF
window.exportToPdf = (title, headers, rows, fileName) => {
    const { jsPDF } = window.jspdf;
    const doc = new jsPDF();

    // Estilo Industrial para el PDF
    doc.setFontSize(18);
    doc.text(title.toUpperCase(), 14, 22);
    doc.setFontSize(10);
    doc.setTextColor(100);
    doc.text(`Generado el: ${new DateTime().toLocaleString()}`, 14, 30);

    doc.autoTable({
        startY: 40,
        head: [headers],
        body: rows,
        theme: 'grid',
        headStyles: { fillColor: [15, 23, 42], fontStyle: 'bold' }, // Slate-900
        styles: { fontSize: 9, cellPadding: 3 }
    });

    doc.save(`${fileName}.pdf`);
}

// Función de impresión simple
window.printWindow = () => {
    window.print();
}