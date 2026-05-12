const translations = {
  en: {
    navEditor: "Derived Icon Editor",
    navGithub: "GitHub",
    languageButton: "PT",
    eyebrow: "Open source Windows context menu power tool",
    heroTitle: "Paste. Customize. Transform. Right from Explorer.",
    heroText:
      "Contextrion adds clipboard-to-file saving, folder icon customization, and a full suite of file and image manipulation tools directly into the Windows Explorer context menu.",
    downloadCta: "Download latest release",
    sourceCta: "View source",
    versionLabel: "Built with:",
    heroPoint1: "Paste clipboard as text, image, ZIP, audio, or binary file",
    heroPoint2: "Customize folder icons with built-in or imported assets",
    heroPoint3: "Resize, crop, watermark, grayscale, optimize images — no extra software needed",
    heroPoint4: "No ads, no tracking, no network calls",
    heroImageAlt: "Contextrion application icon",
    clipboardKicker: "Clipboard to File",
    clipboardTitle: "Turn anything on your clipboard into a file.",
    clipboardText:
      "Right-click in any folder background and choose Paste Into File. Text becomes `.txt`, images become `.png`/`.jpg`, copied files become `.zip`, audio becomes `.wav`, and everything else becomes `.bin`.",
    securityKicker: "Security first",
    securityTitle: "A native tool with a minimal surface area.",
    securityText:
      "Contextrion is a self-contained WinForms application with no telemetry, no ads, and no unnecessary network behavior.",
    sectionEyebrow: "What it does",
    sectionTitle: "Three tools in one context menu entry.",
    feature1Title: "Paste Into File",
    feature1Text:
      "Save clipboard content as a file directly from the Explorer background. Supports text, images (PNG/JPEG format detection), file/folder ZIP archives, audio, and binary data with a preview dialog.",
    feature2Title: "Folder Customization",
    feature2Text:
      "Right-click a folder to apply custom icons from a built-in catalog (Windows 11, 10, 7/8 styles), import your own `.ico`/`.dll`/`.png`/`.jpg` files, or create composite icons with the Derived Icon Editor.",
    feature3Title: "File and Image Tools",
    feature3Text:
      "Apply transformations to selected files directly from the context menu: grayscale, resize, crop, circle crop, watermark, invert colors, clean metadata, optimize for web, minify JS/CSS, rename to URL, enumerate, combine images, and more.",
    feature4Title: "Derived Icon Editor",
    feature4Text:
      "Compose composite icons by layering images with opacity, scale, offset, rotation, and color controls. Available as a web demo and integrated into the app.",
    showcaseEyebrow: "See the windows",
    showcaseTitle: "Preview the actual Contextrion workflow before you install it.",
    showcaseText:
      "Explore the main screens and understand how installation, icon selection, file tools, and custom icon creation work in practice.",
    installerKicker: "Installer",
    installerTitle: "Install, update, and manage the shell integration.",
    installerText:
      "The native installer window keeps all maintenance actions in one place: install or uninstall, open assets, import icons, and legacy registry cleanup.",
    installerDemoCaption:
      "Install Contextrion to enable context menu integration for clipboard-to-file, folder icons, and file tools.",
    installerIconAlt: "Contextrion installer icon preview",
    actionsLabel: "Actions",
    pickerKicker: "Folder Icon Picker",
    pickerTitle: "Browse categories, search icons, and preview choices.",
    pickerText:
      "The picker is organized into categories (Windows 11, 10, 7/8, User Icons, Icon Packs), supports search, shows live previews, and exposes import, restore, and derived icon creation actions.",
    categoryLabel: "Category",
    iconsLabel: "Icons",
    searchLabel: "Search",
    previewLabel: "Preview",
    pickerPreviewText: "Current icon preview",
    pickerPreviewAlt: "Contextrion icon picker preview icon",
    editorKicker: "Derived Icon Editor",
    editorTitle: "Compose new icons with layers, transforms, and color controls.",
    editorText:
      "The derived icon editor lets you stack layers on top of a base folder icon, then tune hue, saturation, opacity, scale, position, rotation, and grayscale colorization.",
    editorOpenLink: "Open the functional editor demo",
    loadBaseLabel: "Load Base Icon",
    addLayerLabel: "Add Layer",
    layersLabel: "Layers",
    controlsLabel: "Controls",
    editorPreviewAlt: "Contextrion derived editor icon preview",
    matrixEyebrow: "Full feature list",
    matrixTitle: "Everything Contextrion can do.",
    matrixClipboard: "Clipboard to File",
    matrixFolder: "Folder Customization",
    matrixImage: "Image Tools",
    matrixFile: "File Tools",
    matrix1: "Text → `.txt`",
    matrix2: "Images (PNG/JPEG format) → `.png`/`.jpg`",
    matrix3: "Files/Folders → `.zip` (preserves structure)",
    matrix4: "Audio → `.wav` (with duration parsing)",
    matrix5: "Other binary → `.bin`",
    matrix6: "Preview dialog before saving",
    matrix7: "Built-in icon catalog (Win 11, 10, 7/8)",
    matrix8: "Import `.ico`, `.dll`, `.png`, `.jpg`",
    matrix9: "Icon packs (DLL/ICO) support",
    matrix10: "Derived Icon Editor (layers, transforms)",
    matrix11: "Restore default folder icon",
    matrix12: "Create timestamp folder hierarchy (YYYY/MM/DD)",
    matrix13: "Grayscale (BT.601 weights)",
    matrix14: "Watermark (text or image, 50% alpha)",
    matrix15: "Crop from center",
    matrix16: "Crop to circle (square + ellipse)",
    matrix17: "Resize (maintains aspect ratio)",
    matrix18: "Invert colors",
    matrix19: "Clean EXIF/metadata",
    matrix20: "Optimize for web (resize + JPEG quality)",
    matrix21: "Combine images (vertical / horizontal)",
    matrix22: "Rename to friendly URL (lowercase, accents removed)",
    matrix23: "Copy as Base64 Data URL",
    matrix24: "Copy file path to clipboard",
    matrix25: "Bulk rename with enumeration pattern",
    matrix26: "Copy file content to clipboard",
    matrix27: "Clean empty directories",
    matrix28: "Minify JS and CSS files",
    stepsEyebrow: "Quick start",
    stepsTitle: "Three steps to start using it.",
    step1: "Download the latest release from GitHub.",
    step2: "Run the app and click Install (requires admin via UAC).",
    step3: "Right-click in a folder or on a file to access all tools.",
    ctaEyebrow: "Free forever",
    ctaTitle: "Use it, inspect it, and adapt it.",
    ctaText:
      "Contextrion is MIT licensed and built for power users who want control over their Windows workspace.",
    ctaButton: "Open the repository",
    supportEyebrow: "Support the developer",
    supportTitle: "Help keep Contextrion alive.",
    supportText:
      "Zonaro is an independent developer maintaining Contextrion on their own, and any amount can help with the project's ongoing maintenance.",
    supportKeyLabel: "Pix key",
    supportQrCaption: "Scan the QR code with your banking app to send support via Pix.",
    supportQrAlt: "Pix QR code for supporting Zonaro",
    pageTitle: "Contextrion | Clipboard to File, Folder Icons, and File Tools for Windows",
    pageDescription:
      "Contextrion is a free open source Windows app that saves clipboard content as files, customizes folder icons, and provides integrated file and image manipulation tools via the Explorer context menu."
  },
  pt: {
    navEditor: "Editor de Ícones Derivados",
    navGithub: "GitHub",
    languageButton: "EN",
    eyebrow: "Ferramenta open source de menu de contexto para Windows",
    heroTitle: "Colar. Personalizar. Transformar. Direto do Explorer.",
    heroText:
      "O Contextrion adiciona salvamento de clipboard como arquivo, personalização de ícones de pasta e um conjunto completo de ferramentas de manipulação de arquivos e imagens diretamente no menu de contexto do Windows Explorer.",
    downloadCta: "Baixar a versão mais recente",
    sourceCta: "Ver código-fonte",
    versionLabel: "Construído com:",
    heroPoint1: "Cole clipboard como texto, imagem, ZIP, áudio ou arquivo binário",
    heroPoint2: "Personalize ícones de pasta com assets nativos ou importados",
    heroPoint3: "Redimensionar, cortar, marca d'água, tons de cinza, otimizar imagens — semsoftware extra",
    heroPoint4: "Sem anúncios, sem rastreamento, sem chamadas de rede",
    heroImageAlt: "Ícone do aplicativo Contextrion",
    clipboardKicker: "Clipboard para Arquivo",
    clipboardTitle: "Transforme qualquer conteúdo da área de transferência em um arquivo.",
    clipboardText:
      "Clique com botão direito no fundo de qualquer pasta e escolha Paste Into File. Texto vira `.txt`, imagens vira `.png`/`.jpg`, arquivos copiados vira `.zip`, áudio vira `.wav`, e todo o resto vira `.bin`.",
    securityKicker: "Segurança primeiro",
    securityTitle: "Uma ferramenta nativa com superfície mínima.",
    securityText:
      "O Contextrion é um aplicativo WinForms independente sem telemetria, sem anúncios e sem comportamento de rede desnecessário.",
    sectionEyebrow: "O que faz",
    sectionTitle: "Três ferramentas em uma entrada do menu de contexto.",
    feature1Title: "Paste Into File",
    feature1Text:
      "Salve o conteúdo da área de transferência como arquivo diretamente do fundo do Explorer. Suporta texto, imagens (detecção de formato PNG/JPEG), arquivos/pastas como ZIP, áudio e dados binários com diálogo de preview.",
    feature2Title: "Personalização de Pastas",
    feature2Text:
      "Clique com botão direito em uma pasta para aplicar ícones personalizados de um catálogo nativo (estilos Windows 11, 10, 7/8), importar seus próprios arquivos `.ico`/`.dll`/`.png`/`.jpg`, ou criar ícones compostos com o Editor de Ícones Derivados.",
    feature3Title: "Ferramentas de Arquivo e Imagem",
    feature3Text:
      "Aplique transformações nos arquivos selecionados diretamente do menu de contexto: tons de cinza, redimensionar, cortar, corte circular, marca d'água, inverter cores, limpar metadados, otimizar para web, minificar JS/CSS, renomear para URL, enumerar, combinar imagens e mais.",
    feature4Title: "Editor de Ícones Derivados",
    feature4Text:
      "Componha ícones compostos empilhando imagens com opacidade, escala, deslocamento, rotação e controles de cor. Disponível como demo web e integrado ao app.",
    showcaseEyebrow: "Veja as janelas",
    showcaseTitle: "Visualize o fluxo real do Contextrion antes mesmo de instalar.",
    showcaseText:
      "Explore as telas principais e entenda na prática como funcionam a instalação, seleção de ícones, ferramentas de arquivo e criação de ícones personalizados.",
    installerKicker: "Instalador",
    installerTitle: "Instale, atualize e gerencie a integração com o shell.",
    installerText:
      "A janela nativa do instalador reúne todas as ações de manutenção em um só lugar: instalar ou desinstalar, abrir assets, importar ícones e limpeza de registros legados.",
    installerDemoCaption:
      "Instale o Contextrion para ativar a integração com o menu de contexto para clipboard-to-file, ícones de pasta e ferramentas de arquivo.",
    installerIconAlt: "Preview do ícone do instalador do Contextrion",
    actionsLabel: "Ações",
    pickerKicker: "Seletor de Ícone de Pasta",
    pickerTitle: "Navegue por categorias, pesquise ícones e visualize escolhas.",
    pickerText:
      "O seletor é organizado em categorias (Windows 11, 10, 7/8, Ícones do Usuário, Pacotes de Ícones), suporta busca, mostra previews ao vivo e expõe ações de importar, restaurar e criar ícones derivados.",
    categoryLabel: "Categoria",
    iconsLabel: "Ícones",
    searchLabel: "Busca",
    previewLabel: "Preview",
    pickerPreviewText: "Preview do ícone atual",
    pickerPreviewAlt: "Preview do ícone no seletor do Contextrion",
    editorKicker: "Editor de Ícones Derivados",
    editorTitle: "Monte novos ícones com camadas, transformações e controles de cor.",
    editorText:
      "O editor de ícones derivados permite empilhar camadas sobre um ícone base de pasta e ajustar matiz, saturação, opacidade, escala, posição, rotação e colorização de tons de cinza.",
    editorOpenLink: "Abrir a demo funcional do editor",
    loadBaseLabel: "Carregar Ícone Base",
    addLayerLabel: "Adicionar Camada",
    layersLabel: "Camadas",
    controlsLabel: "Controles",
    editorPreviewAlt: "Preview do editor derivado do Contextrion",
    matrixEyebrow: "Lista completa de recursos",
    matrixTitle: "Tudo o que o Contextrion pode fazer.",
    matrixClipboard: "Clipboard para Arquivo",
    matrixFolder: "Personalização de Pastas",
    matrixImage: "Ferramentas de Imagem",
    matrixFile: "Ferramentas de Arquivo",
    matrix1: "Texto → `.txt`",
    matrix2: "Imagens (formato PNG/JPEG) → `.png`/`.jpg`",
    matrix3: "Arquivos/Pastas → `.zip` (preserva estrutura)",
    matrix4: "Áudio → `.wav` (com parsing de duração)",
    matrix5: "Outros binários → `.bin`",
    matrix6: "Diálogo de preview antes de salvar",
    matrix7: "Catálogo de ícones nativo (Win 11, 10, 7/8)",
    matrix8: "Importar `.ico`, `.dll`, `.png`, `.jpg`",
    matrix9: "Suporte a pacotes de ícones (DLL/ICO)",
    matrix10: "Editor de Ícones Derivados (camadas, transformações)",
    matrix11: "Restaurar ícone padrão da pasta",
    matrix12: "Criar hierarquia de pastas por timestamp (AAAA/MM/DD)",
    matrix13: "Tons de cinza (pesos BT.601)",
    matrix14: "Marca d'água (texto ou imagem, 50% alpha)",
    matrix15: "Cortar a partir do centro",
    matrix16: "Cortar para círculo (quadrado + elipse)",
    matrix17: "Redimensionar (mantém proporção)",
    matrix18: "Inverter cores",
    matrix19: "Limpar EXIF/metadata",
    matrix20: "Otimizar para web (redimensionar + qualidade JPEG)",
    matrix21: "Combinar imagens (vertical / horizontal)",
    matrix22: "Renomear para URL amigável (minúsculas, sem acentos)",
    matrix23: "Copiar como Data URL Base64",
    matrix24: "Copiar caminho do arquivo para clipboard",
    matrix25: "Renomeação em massa com padrão de enumeração",
    matrix26: "Copiar conteúdo do arquivo para clipboard",
    matrix27: "Limpar diretórios vazios",
    matrix28: "Minificar arquivos JS e CSS",
    stepsEyebrow: "Início rápido",
    stepsTitle: "Três passos para começar a usar.",
    step1: "Baixe a versão mais recente do GitHub.",
    step2: "Execute o app e clique em Install (requer admin via UAC).",
    step3: "Clique com botão direito em uma pasta ou arquivo para acessar todas as ferramentas.",
    ctaEyebrow: "Grátis para sempre",
    ctaTitle: "Use, inspecione e adapte.",
    ctaText:
      "O Contextrion usa licença MIT e foi feito para power users que querem controle sobre o próprio espaço de trabalho no Windows.",
    ctaButton: "Abrir o repositório",
    supportEyebrow: "Apoie o desenvolvedor",
    supportTitle: "Ajude a manter o Contextrion vivo.",
    supportText:
      "Zonaro é um desenvolvedor independente e mantém o Contextrion por conta própria, e qualquer valor já ajuda na manutenção contínua do projeto.",
    supportKeyLabel: "Chave Pix",
    supportQrCaption: "Escaneie o QR Code no app do seu banco para enviar um apoio via Pix.",
    supportQrAlt: "QR Code Pix para apoiar o Zonaro",
    pageTitle: "Contextrion | Clipboard para Arquivo, Ícones de Pasta e Ferramentas de Arquivo para Windows",
    pageDescription:
      "Contextrion é um app gratuito e open source para Windows que salva o conteúdo da área de transferência como arquivos, personaliza ícones de pasta e fornece ferramentas integradas de manipulação de arquivos e imagens via menu de contexto do Explorer."
  }
};

const supportedLanguages = ["en", "pt"];
const storedLanguage = window.localStorage.getItem("contextrion-language");
const browserLanguage = (navigator.languages && navigator.languages[0]) || navigator.language || "en";
const pixKey = "43077469880";
const pixMerchantName = "ZONARO";
const pixMerchantCity = "SAO PAULO";

function currentLanguage() {
  return document.documentElement.lang || "en";
}

function normalizeLanguage(languageCode) {
  const shortCode = languageCode.toLowerCase().slice(0, 2);
  return supportedLanguages.includes(shortCode) ? shortCode : "en";
}

function formatPixField(id, value) {
  const stringValue = String(value);
  return `${id}${stringValue.length.toString().padStart(2, "0")}${stringValue}`;
}

function computePixCrc16(payload) {
  let crc = 0xffff;

  for (let index = 0; index < payload.length; index += 1) {
    crc ^= payload.charCodeAt(index) << 8;

    for (let bit = 0; bit < 8; bit += 1) {
      crc = (crc & 0x8000) !== 0 ? ((crc << 1) ^ 0x1021) : (crc << 1);
      crc &= 0xffff;
    }
  }

  return crc.toString(16).toUpperCase().padStart(4, "0");
}

function buildPixPayload() {
  const merchantAccount = formatPixField(
    "26",
    `${formatPixField("00", "BR.GOV.BCB.PIX")}${formatPixField("01", pixKey)}`
  );
  const additionalData = formatPixField("62", formatPixField("05", "***"));
  const payloadWithoutCrc = [
    formatPixField("00", "01"),
    formatPixField("01", "11"),
    merchantAccount,
    formatPixField("52", "0000"),
    formatPixField("53", "986"),
    formatPixField("58", "BR"),
    formatPixField("59", pixMerchantName),
    formatPixField("60", pixMerchantCity),
    additionalData,
    "6304"
  ].join("");

  return `${payloadWithoutCrc}${computePixCrc16(payloadWithoutCrc)}`;
}

function renderPixQrCode() {
  const qrImage = document.getElementById("pix-qr-image");

  if (!qrImage) {
    return;
  }

  const payload = buildPixPayload();
  qrImage.src = `https://api.qrserver.com/v1/create-qr-code/?size=320x320&data=${encodeURIComponent(payload)}`;
}

function updatePageLanguage(language) {
  const content = translations[language];

  document.documentElement.lang = language;
  document.title = content.pageTitle;

  const description = document.querySelector('meta[name="description"]');
  if (description) {
    description.setAttribute("content", content.pageDescription);
  }

  document.querySelectorAll("[data-i18n]").forEach((element) => {
    const key = element.dataset.i18n;
    if (content[key]) {
      element.textContent = content[key];
    }
  });

  document.querySelectorAll("[data-i18n-alt]").forEach((element) => {
    const key = element.dataset.i18nAlt;
    if (content[key]) {
      element.setAttribute("alt", content[key]);
    }
  });

  window.localStorage.setItem("contextrion-language", language);
}

const initialLanguage = normalizeLanguage(storedLanguage || browserLanguage);
updatePageLanguage(initialLanguage);
renderPixQrCode();

document.getElementById("language-switch").addEventListener("click", () => {
  const nextLanguage = document.documentElement.lang === "pt" ? "en" : "pt";
  updatePageLanguage(nextLanguage);
});
